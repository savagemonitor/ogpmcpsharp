using System.ComponentModel;
using System.Text.Json;
using GpSharp.Data;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenGoPro.Client;

namespace HttpMcpServer.Tools;

[McpServerToolType]
public sealed class CameraTools(IGpSharpAdapter adapter, ILogger<CameraTools> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "list_cameras", ReadOnly = true, Destructive = false, Idempotent = true),
     Description("Discover GoPro cameras on the local network for up to five seconds. Returns camera identifiers and network endpoints.")]
    public async Task<string> ListCameras(CancellationToken cancellationToken) =>
        JsonSerializer.Serialize(await ExecuteAsync(
            () => adapter.ListCamerasAsync(cancellationToken), "Camera discovery"), JsonOptions);

    [McpServerTool(Name = "list_media", ReadOnly = true, Destructive = false, Idempotent = true),
     Description("List media on a previously discovered GoPro camera. Reads camera data.")]
    public async Task<string> ListMedia(
        [Description("Camera ID returned by list_cameras.")] string cameraId)
    {
        ValidateCameraId(cameraId);
        MediaList media = await ExecuteAsync(() => adapter.GetMediaListAsync(cameraId), "Media listing");
        return JsonSerializer.Serialize(media, JsonOptions);
    }

    [McpServerTool(Name = "shutter_start", ReadOnly = false, Destructive = true, Idempotent = false),
     Description("Start the shutter on a previously discovered GoPro camera. This changes camera state and may start recording or capture a photo.")]
    public async Task<string> ShutterStart(
        [Description("Camera ID returned by list_cameras.")] string cameraId)
    {
        ValidateCameraId(cameraId);
        await ExecuteAsync(async () => { await adapter.SetShutterAsync(cameraId, true); return true; }, "Shutter start");
        return "Shutter start command completed.";
    }

    [McpServerTool(Name = "shutter_stop", ReadOnly = false, Destructive = true, Idempotent = false),
     Description("Stop the shutter on a previously discovered GoPro camera. This changes camera state and may stop recording.")]
    public async Task<string> ShutterStop(
        [Description("Camera ID returned by list_cameras.")] string cameraId)
    {
        ValidateCameraId(cameraId);
        await ExecuteAsync(async () => { await adapter.SetShutterAsync(cameraId, false); return true; }, "Shutter stop");
        return "Shutter stop command completed.";
    }

    private static void ValidateCameraId(string cameraId)
    {
        if (string.IsNullOrWhiteSpace(cameraId))
        {
            throw new McpException("A camera ID from list_cameras is required.");
        }
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string operation)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            logger.LogWarning("{Operation} failed with {ErrorType}.", operation, error.GetType().Name);
            throw new McpException($"{operation} failed. Check that the camera was discovered and is reachable.");
        }
    }
}
