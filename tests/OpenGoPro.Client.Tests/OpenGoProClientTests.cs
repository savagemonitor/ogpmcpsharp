using System;
using System.Text.Json;
using System.Threading.Tasks;
using GpSharp.Data;
using Moq;
using OpenGoPro.Client;
using Xunit;

namespace OpenGoPro.Client.Tests;

public class OpenGoProClientTests
{
    [Fact]
    public async Task SendMcpCommandAsync_ListCameras_ReturnsDiscoveredCameraDetails()
    {
        var sdk = new Mock<IGpSharpAdapter>();
        sdk.Setup(x => x.ListCamerasAsync(default))
            .ReturnsAsync(new[]
            {
                new CameraSummary("1234", "GoPro", "HERO", "192.168.1.2", 8080, "Unknown", "Wifi")
            });
        var client = new OpenGoProClient(sdk.Object);

        var result = await client.SendMcpCommandAsync("""{"name":"list_cameras"}""");

        Assert.Contains("\"id\":\"1234\"", result);
        Assert.Contains("\"ipAddress\":\"192.168.1.2\"", result);
    }

    [Theory]
    [InlineData("shutter_start", true)]
    [InlineData("shutter_stop", false)]
    public async Task SendMcpCommandAsync_ShutterCommand_UsesCameraAndRequestedState(
        string name,
        bool expectedState)
    {
        var sdk = new Mock<IGpSharpAdapter>();
        sdk.Setup(x => x.SetShutterAsync("1234", expectedState)).Returns(Task.CompletedTask);
        var client = new OpenGoProClient(sdk.Object);
        var command = JsonSerializer.Serialize(new { name, cameraId = "1234" });

        var result = await client.SendMcpCommandAsync(command);

        Assert.Equal("""{"success":true}""", result);
        sdk.Verify(x => x.SetShutterAsync("1234", expectedState), Times.Once);
    }

    [Fact]
    public async Task SendMcpCommandAsync_ListMedia_ReturnsSdkMediaResult()
    {
        var sdk = new Mock<IGpSharpAdapter>();
        sdk.Setup(x => x.GetMediaListAsync("1234"))
            .ReturnsAsync(new MediaList { Media = Array.Empty<MediaDirectory>() });
        var client = new OpenGoProClient(sdk.Object);

        var result = await client.SendMcpCommandAsync("""{"name":"list_media","cameraId":"1234"}""");

        Assert.Contains("\"media\":[]", result);
    }

    [Fact]
    public async Task SendMcpCommandAsync_SettingAndStatusCalls_AreValidatedAndDispatched()
    {
        var sdk = new Mock<IGpSharpAdapter>(MockBehavior.Strict);
        sdk.Setup(x => x.GetSettingAsync("1234", 2, default)).ReturnsAsync(JsonSerializer.SerializeToElement(9));
        sdk.Setup(x => x.GetStatusAsync("1234", 70, default)).ReturnsAsync(JsonSerializer.SerializeToElement(83));
        sdk.Setup(x => x.SetSettingAsync("1234", 2, 9, default)).Returns(Task.CompletedTask);
        var client = new OpenGoProClient(sdk.Object);
        Assert.Equal("9", await client.SendMcpCommandAsync("""{"name":"get_setting","cameraId":"1234","settingId":2}"""));
        Assert.Equal("83", await client.SendMcpCommandAsync("""{"name":"get_status","cameraId":"1234","statusId":70}"""));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.SendMcpCommandAsync("""{"name":"set_setting","cameraId":"1234","settingId":2,"option":9}"""));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.SendMcpCommandAsync("""{"name":"set_setting","cameraId":"1234","settingId":-1,"option":9,"confirm":true}"""));
        Assert.Equal("""{"success":true}""", await client.SendMcpCommandAsync(
            """{"name":"set_setting","cameraId":"1234","settingId":2,"option":9,"confirm":true}"""));
        sdk.VerifyAll();
    }

    [Theory]
    [InlineData("set_camera_name")]
    [InlineData("get_last_media")]
    [InlineData("set_turbo_active")]
    [InlineData("get_cohn_status")]
    [InlineData("create_cohn_cert")]
    [InlineData("clear_cohn_cert")]
    public async Task SendMcpCommandAsync_ProtoOnlyOperation_ReportsUnsupported(string name)
    {
        var client = new OpenGoProClient(Mock.Of<IGpSharpAdapter>());
        var command = JsonSerializer.Serialize(new { name, cameraId = "1234" });

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => client.SendMcpCommandAsync(command));

        Assert.Contains("not implemented", exception.Message);
    }

    [Fact]
    public async Task SendMcpCommandAsync_InvalidJson_ReportsInvalidRequest()
    {
        var client = new OpenGoProClient(Mock.Of<IGpSharpAdapter>());

        await Assert.ThrowsAsync<ArgumentException>(() => client.SendMcpCommandAsync("not-json"));
    }
}
