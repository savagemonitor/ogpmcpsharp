using System.Net;
using System.Text.Json;
using System.Runtime.CompilerServices;
using GpSharp.Core;
using GpSharp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Moq;
using OpenGoPro.Client;

namespace HttpMcpServer.Tests;

public sealed class McpProtocolTests
{
    [Fact]
    public async Task ClientInitializesListsAndCallsSupportedTools()
    {
        var adapter = new Mock<IGpSharpAdapter>(MockBehavior.Strict);
        adapter.Setup(x => x.ListCamerasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CameraSummary("test-id", "Test", "Test", "127.0.0.1", 80, "test", null)]);
        adapter.Setup(x => x.GetMediaListAsync("test-id")).ReturnsAsync(new MediaList());
        adapter.Setup(x => x.SetShutterAsync("test-id", true)).Returns(Task.CompletedTask);
        adapter.Setup(x => x.SetShutterAsync("test-id", false)).Returns(Task.CompletedTask);
        using var factory = CreateFactory(adapter.Object);
        using var http = factory.CreateClient();
        await using var client = await ConnectAsync(http);

        Assert.NotNull(client.NegotiatedProtocolVersion);
        Assert.Null(client.SessionId);
        var tools = await client.ListToolsAsync();
        Assert.Equal(new[] { "list_cameras", "list_media", "shutter_start", "shutter_stop" },
            tools.Select(t => t.Name).OrderBy(n => n).ToArray());
        Assert.Contains("recording", tools.Single(t => t.Name == "shutter_start").Description);
        var cameras = await client.CallToolAsync("list_cameras");
        Assert.NotEqual(true, cameras.IsError);
        var json = JsonDocument.Parse(Assert.Single(cameras.Content.OfType<TextContentBlock>()).Text);
        Assert.Equal("test-id", json.RootElement[0].GetProperty("id").GetString());
        var arguments = new Dictionary<string, object?> { ["cameraId"] = "test-id" };
        var media = await client.CallToolAsync("list_media", arguments);
        Assert.NotEqual(true, media.IsError);
        Assert.Equal(0, JsonDocument.Parse(Assert.Single(media.Content.OfType<TextContentBlock>()).Text)
            .RootElement.GetProperty("media").GetArrayLength());
        Assert.NotEqual(true, (await client.CallToolAsync("shutter_start", arguments)).IsError);
        Assert.NotEqual(true, (await client.CallToolAsync("shutter_stop", arguments)).IsError);
        adapter.VerifyAll();
    }

    [Fact]
    public async Task FailedAndUnsupportedCallsAreNotReportedAsSuccess()
    {
        var adapter = new Mock<IGpSharpAdapter>(MockBehavior.Strict);
        adapter.Setup(x => x.GetMediaListAsync("bad")).ThrowsAsync(new InvalidOperationException("private detail"));
        using var factory = CreateFactory(adapter.Object);
        using var http = factory.CreateClient();
        await using var client = await ConnectAsync(http);
        var result = await client.CallToolAsync("list_media", new Dictionary<string, object?> { ["cameraId"] = "bad" });
        Assert.True(result.IsError);
        Assert.DoesNotContain("private detail", Assert.Single(result.Content.OfType<TextContentBlock>()).Text);
        var invalid = await client.CallToolAsync("shutter_start", new Dictionary<string, object?> { ["cameraId"] = "" });
        Assert.True(invalid.IsError);
        await Assert.ThrowsAsync<McpProtocolException>(async () => await client.CallToolAsync("set_turbo_active"));
    }

    [Fact]
    public async Task DiscoveryCancellationReachesAdapter()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapter = new Mock<IGpSharpAdapter>(MockBehavior.Strict);
        adapter.Setup(x => x.ListCamerasAsync(It.IsAny<CancellationToken>())).Returns(async (CancellationToken token) =>
        {
            started.SetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) { cancelled.SetResult(); throw; }
            return Array.Empty<CameraSummary>();
        });
        using var factory = CreateFactory(adapter.Object);
        using var http = factory.CreateClient();
        await using var client = await ConnectAsync(http);
        using var cancellation = new CancellationTokenSource();
        var call = client.CallToolAsync("list_cameras", cancellationToken: cancellation.Token).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        var error = await Record.ExceptionAsync(() => call);
        Assert.True(error is OperationCanceledException ||
            error is HttpRequestException { StatusCode: (HttpStatusCode)499 });
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task EndpointIsStatelessAndRejectsUntrustedHosts()
    {
        using var factory = CreateFactory(Mock.Of<IGpSharpAdapter>());
        using var http = factory.CreateClient();
        using var response = await http.GetAsync("/mcp");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Host = "untrusted.example";
        using var rejected = await http.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
    }

    [Fact]
    public async Task InitializeSupportsCurrentStableProtocolWithoutSession()
    {
        using var factory = CreateFactory(Mock.Of<IGpSharpAdapter>());
        using var http = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Content = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}""",
            System.Text.Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("Mcp-Session-Id"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("2025-11-25", body);
        Assert.Contains("\"id\":1", body);
    }

    [Fact]
    public async Task RepeatedDiscoveryCallsUseTheSdkDiscoveryService()
    {
        var discovery = new SnapshotDiscovery();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDiscoveryService>();
                services.AddSingleton<IDiscoveryService>(discovery);
            }));
        using var http = factory.CreateClient();
        await using var client = await ConnectAsync(http);

        foreach (var _ in Enumerable.Range(0, 2))
        {
            var result = await client.CallToolAsync("list_cameras");
            Assert.NotEqual(true, result.IsError);
            using var cameras = JsonDocument.Parse(Assert.Single(result.Content.OfType<TextContentBlock>()).Text);
            Assert.Equal("1234", Assert.Single(cameras.RootElement.EnumerateArray()).GetProperty("id").GetString());
        }

        discovery.Available = false;
        var removed = await client.CallToolAsync("list_cameras");
        Assert.NotEqual(true, removed.IsError);
        using var remaining = JsonDocument.Parse(Assert.Single(removed.Content.OfType<TextContentBlock>()).Text);
        Assert.Equal(0, remaining.RootElement.GetArrayLength());
    }

    private sealed class SnapshotDiscovery : IDiscoveryService
    {
        public bool Available { get; set; } = true;

        public async IAsyncEnumerable<DiscoveredDevice> DiscoverAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (Available)
            {
                yield return new DiscoveredDevice(
                    new GoProId("1234"), "Test camera", "192.0.2.1", 80, "Unknown", "Unknown");
            }
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(IGpSharpAdapter adapter) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGpSharpAdapter>();
                services.AddSingleton(adapter);
            }));

    private static Task<McpClient> ConnectAsync(HttpClient http) =>
        McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }, http));
}
