using System.ComponentModel;
using System.Text.Json;
using GpSharp.Data;
using GpSharp.GoPro.Settings;
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

    [McpServerTool(Name = "get_camera_state", ReadOnly = true, Destructive = false, Idempotent = true),
     Description("Read a snapshot of all settings and status values reported by a previously discovered camera, keyed by numeric Open GoPro ID.")]
    public async Task<string> GetCameraState(
        [Description("Camera ID returned by list_cameras.")] string cameraId,
        CancellationToken cancellationToken)
    {
        ValidateCameraId(cameraId);
        return JsonSerializer.Serialize(await ExecuteAsync(
            () => adapter.GetCameraStateAsync(cameraId, cancellationToken), "Camera state read"), JsonOptions);
    }

    [McpServerTool(Name = "get_setting", ReadOnly = true, Destructive = false, Idempotent = true),
     Description("Read a camera setting option by Open GoPro numeric setting ID. Available IDs and values depend on the camera.")]
    public async Task<string> GetSetting(
        [Description("Camera ID returned by list_cameras.")] string cameraId,
        [Description("Open GoPro setting ID from 0 to 255.")] int settingId,
        CancellationToken cancellationToken)
    {
        ValidateCameraId(cameraId);
        ValidateId(settingId, "Setting");
        return JsonSerializer.Serialize(await ExecuteAsync(
            () => adapter.GetSettingAsync(cameraId, settingId, cancellationToken), "Setting read"), JsonOptions);
    }

    [McpServerTool(Name = "get_status", ReadOnly = true, Destructive = false, Idempotent = true),
     Description("Read a status value by Open GoPro numeric status ID from a fresh camera-state snapshot.")]
    public async Task<string> GetStatus(
        [Description("Camera ID returned by list_cameras.")] string cameraId,
        [Description("Open GoPro status ID from 0 to 255.")] int statusId,
        CancellationToken cancellationToken)
    {
        ValidateCameraId(cameraId);
        ValidateId(statusId, "Status");
        return JsonSerializer.Serialize(await ExecuteAsync(
            () => adapter.GetStatusAsync(cameraId, statusId, cancellationToken), "Status read"), JsonOptions);
    }

    [McpServerTool(Name = "set_setting", ReadOnly = false, Destructive = true, Idempotent = false),
     Description("Change a camera setting. This modifies camera configuration. Ask the user for explicit confirmation of the camera, setting ID, and option before calling; confirm must be true. Supported options depend on the camera.")]
    public async Task<string> SetSetting(
        [Description("Camera ID returned by list_cameras.")] string cameraId,
        [Description("Open GoPro setting ID from 0 to 255.")] int settingId,
        [Description("Non-negative integer option value supported by the camera.")] long option,
        [Description("True only after the user explicitly confirms this exact setting change.")] bool confirm,
        CancellationToken cancellationToken)
    {
        ValidateCameraId(cameraId);
        ValidateId(settingId, "Setting");
        if (option < 0)
            throw new McpException("Setting option must be a non-negative integer.");
        if (!confirm)
            throw new McpException("Explicit user confirmation is required before changing a setting.");
        await ExecuteAsync(async () =>
        {
            await adapter.SetSettingAsync(cameraId, settingId, option, cancellationToken);
            return true;
        }, "Setting update");
        return "Setting update command completed.";
    }

    private static void ValidateId(int id, string label)
    {
        if (id is < 0 or > 255)
            throw new McpException($"{label} ID must be between 0 and 255.");
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
        catch (SettingRejectedException error)
        {
            var options = JsonSerializer.Serialize(error.SupportedOptions, JsonOptions);
            throw new McpException($"Camera rejected setting {error.SettingId}, option {error.OptionId} (error {error.ErrorCode}). Supported options: {options}");
        }
        catch (KeyNotFoundException)
        {
            throw new McpException($"{operation} failed: the camera did not report the requested ID.");
        }
        catch (Exception error)
        {
            logger.LogWarning("{Operation} failed with {ErrorType}.", operation, error.GetType().Name);
            throw new McpException($"{operation} failed. Check that the camera was discovered and is reachable.");
        }
    }
}
