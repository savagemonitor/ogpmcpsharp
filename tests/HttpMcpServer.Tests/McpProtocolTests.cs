using System.Net;
using System.Text.Json;
using System.Runtime.CompilerServices;
using GpSharp.Core;
using GpSharp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
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
        Assert.Equal(new[] { "get_camera_state", "get_setting", "get_status", "list_cameras", "list_media", "set_setting", "shutter_start", "shutter_stop" },
            tools.Select(t => t.Name).OrderBy(n => n).ToArray());
        Assert.Contains("recording", tools.Single(t => t.Name == "shutter_start").Description);
        Assert.Contains("explicit confirmation", tools.Single(t => t.Name == "set_setting").Description);
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
    public async Task SettingsAndStatusToolsValidateAndCallAdapter()
    {
        var adapter = new Mock<IGpSharpAdapter>(MockBehavior.Strict);
        adapter.Setup(x => x.GetSettingAsync("test-id", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.SerializeToElement(9));
        adapter.Setup(x => x.GetStatusAsync("test-id", 70, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.SerializeToElement(83));
        adapter.Setup(x => x.SetSettingAsync("test-id", 2, 9, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        using var factory = CreateFactory(adapter.Object);
        using var http = factory.CreateClient();
        await using var client = await ConnectAsync(http);

        var settingArgs = new Dictionary<string, object?> { ["cameraId"] = "test-id", ["settingId"] = 2 };
        var setting = await client.CallToolAsync("get_setting", settingArgs);
        Assert.NotEqual(true, setting.IsError);
        Assert.Equal("9", Assert.Single(setting.Content.OfType<TextContentBlock>()).Text);
        var status = await client.CallToolAsync("get_status",
            new Dictionary<string, object?> { ["cameraId"] = "test-id", ["statusId"] = 70 });
        Assert.NotEqual(true, status.IsError);
        Assert.Equal("83", Assert.Single(status.Content.OfType<TextContentBlock>()).Text);

        var args = new Dictionary<string, object?> { ["cameraId"] = "test-id", ["settingId"] = 2, ["option"] = 9, ["confirm"] = false };
        Assert.True((await client.CallToolAsync("set_setting", args)).IsError);
        args["confirm"] = true;
        args["option"] = -1;
        Assert.True((await client.CallToolAsync("set_setting", args)).IsError);
        args["option"] = 9;
        args["settingId"] = -1;
        Assert.True((await client.CallToolAsync("set_setting", args)).IsError);
        args["settingId"] = 2;
        Assert.NotEqual(true, (await client.CallToolAsync("set_setting", args)).IsError);
        adapter.VerifyAll();
        adapter.Verify(x => x.SetSettingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SettingAndStatusFailuresReturnErrors()
    {
        var adapter = new Mock<IGpSharpAdapter>(MockBehavior.Strict);
        adapter.Setup(x => x.GetStatusAsync("test-id", 70, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("private details"));
        adapter.Setup(x => x.SetSettingAsync("test-id", 2, 9, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("private details"));
        using var factory = CreateFactory(adapter.Object);
        using var http = factory.CreateClient();
        await using var client = await ConnectAsync(http);
        var status = await client.CallToolAsync("get_status",
            new Dictionary<string, object?> { ["cameraId"] = "test-id", ["statusId"] = 70 });
        var set = await client.CallToolAsync("set_setting",
            new Dictionary<string, object?> { ["cameraId"] = "test-id", ["settingId"] = 2, ["option"] = 9, ["confirm"] = true });
        Assert.True(status.IsError);
        Assert.True(set.IsError);
        Assert.DoesNotContain("private details", Assert.Single(set.Content.OfType<TextContentBlock>()).Text);
    }

    [Fact]
    public async Task SettingsToolsExerciseRealAdapterAgainstFakeCamera()
    {
        var fakeBuilder = WebApplication.CreateSlimBuilder();
        fakeBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var camera = fakeBuilder.Build();
        var writes = 0;
        camera.MapGet("/gopro/camera/state", () =>
            Results.Text("""{"status":{"70":83,"270":3},"settings":{"2":9,"275":1}}""", "application/json"));
        camera.MapGet("/gopro/camera/setting", (HttpRequest request) =>
        {
            if (!request.Query.ContainsKey("option"))
                return Results.Text("""{"option":9}""", "application/json");
            Interlocked.Increment(ref writes);
            return request.Query["option"] == "7"
                ? Results.Text("""{"error":3,"setting_id":2,"option_id":7,"supported_options":[{"id":9,"display_name":"4K"}]}""",
                    "application/json", statusCode: 403)
                : Results.Text("{}");
        });
        await camera.StartAsync();
        var address = camera.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var discovery = new SnapshotDiscovery { Port = new Uri(address).Port, Address = "127.0.0.1" };
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDiscoveryService>();
                services.AddSingleton<IDiscoveryService>(discovery);
            }));
        using var http = factory.CreateClient();
        await using var client = await ConnectAsync(http);
        Assert.NotEqual(true, (await client.CallToolAsync("list_cameras")).IsError);
        var state = await client.CallToolAsync("get_camera_state", new Dictionary<string, object?> { ["cameraId"] = "1234" });
        using var stateJson = JsonDocument.Parse(Assert.Single(state.Content.OfType<TextContentBlock>()).Text);
        Assert.Equal(83, stateJson.RootElement.GetProperty("statuses").GetProperty("70").GetInt32());
        Assert.Equal(9, stateJson.RootElement.GetProperty("settings").GetProperty("2").GetInt32());
        Assert.Equal(1, stateJson.RootElement.GetProperty("settings").GetProperty("275").GetInt32());
        Assert.Equal(3, stateJson.RootElement.GetProperty("statuses").GetProperty("270").GetInt32());
        var setting = await client.CallToolAsync("get_setting",
            new Dictionary<string, object?> { ["cameraId"] = "1234", ["settingId"] = 2 });
        Assert.Equal("9", Assert.Single(setting.Content.OfType<TextContentBlock>()).Text);
        var status = await client.CallToolAsync("get_status",
            new Dictionary<string, object?> { ["cameraId"] = "1234", ["statusId"] = 70 });
        Assert.Equal("83", Assert.Single(status.Content.OfType<TextContentBlock>()).Text);
        var extendedStatus = await client.CallToolAsync("get_status",
            new Dictionary<string, object?> { ["cameraId"] = "1234", ["statusId"] = 270 });
        Assert.Equal("3", Assert.Single(extendedStatus.Content.OfType<TextContentBlock>()).Text);
        var missingStatus = await client.CallToolAsync("get_status",
            new Dictionary<string, object?> { ["cameraId"] = "1234", ["statusId"] = 71 });
        Assert.True(missingStatus.IsError);
        Assert.Contains("did not report", Assert.Single(missingStatus.Content.OfType<TextContentBlock>()).Text);
        var args = new Dictionary<string, object?> { ["cameraId"] = "1234", ["settingId"] = 2, ["option"] = 7, ["confirm"] = true };
        var rejected = await client.CallToolAsync("set_setting", args);
        Assert.True(rejected.IsError);
        Assert.Contains("4K", Assert.Single(rejected.Content.OfType<TextContentBlock>()).Text);
        args["option"] = 9;
        Assert.NotEqual(true, (await client.CallToolAsync("set_setting", args)).IsError);
        Assert.Equal(2, writes);
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
    public async Task SettingReadCancellationReachesAdapter()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapter = new Mock<IGpSharpAdapter>(MockBehavior.Strict);
        adapter.Setup(x => x.GetSettingAsync("test-id", 2, It.IsAny<CancellationToken>()))
            .Returns(async (string _, int _, CancellationToken token) =>
            {
                started.SetResult();
                try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
                catch (OperationCanceledException) { cancelled.SetResult(); throw; }
                return JsonSerializer.SerializeToElement(0);
            });
        using var factory = CreateFactory(adapter.Object);
        using var http = factory.CreateClient();
        await using var client = await ConnectAsync(http);
        using var cancellation = new CancellationTokenSource();
        var call = client.CallToolAsync("get_setting",
            new Dictionary<string, object?> { ["cameraId"] = "test-id", ["settingId"] = 2 },
            cancellationToken: cancellation.Token).AsTask();
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
        public int Port { get; set; } = 80;
        public string Address { get; set; } = "192.0.2.1";

        public async IAsyncEnumerable<DiscoveredDevice> DiscoverAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (Available)
            {
                yield return new DiscoveredDevice(
                    new GoProId("1234"), "Test camera", Address, Port, "Unknown", "Unknown");
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
