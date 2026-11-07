using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenGoPro.Client;

public sealed class OpenGoProClient : IOpenGoProClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IGpSharpAdapter _sdk;

    public OpenGoProClient(IGpSharpAdapter sdk)
    {
        _sdk = sdk;
    }

    public async Task<string> SendMcpCommandAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("A JSON command is required.", nameof(command));
        }

        CameraCommand request;
        try
        {
            request = JsonSerializer.Deserialize<CameraCommand>(command, JsonOptions)
                ?? throw new ArgumentException("The command JSON cannot be null.", nameof(command));
        }
        catch (JsonException error)
        {
            throw new ArgumentException("The command must contain valid JSON.", nameof(command), error);
        }

        return request.Name switch
        {
            "list_cameras" => JsonSerializer.Serialize(await _sdk.ListCamerasAsync(default), JsonOptions),
            "list_media" => JsonSerializer.Serialize(
                await _sdk.GetMediaListAsync(RequireCameraId(request.CameraId)), JsonOptions),
            "shutter_start" => await SetShutterAsync(RequireCameraId(request.CameraId), true),
            "shutter_stop" => await SetShutterAsync(RequireCameraId(request.CameraId), false),
            "get_camera_state" => JsonSerializer.Serialize(
                await _sdk.GetCameraStateAsync(RequireCameraId(request.CameraId)), JsonOptions),
            "get_setting" => JsonSerializer.Serialize(
                await _sdk.GetSettingAsync(RequireCameraId(request.CameraId), RequireId(request.SettingId)), JsonOptions),
            "get_status" => JsonSerializer.Serialize(
                await _sdk.GetStatusAsync(RequireCameraId(request.CameraId), RequireId(request.StatusId)), JsonOptions),
            "set_setting" => await SetSettingAsync(request),
            "set_camera_name" or "get_last_media" or "set_turbo_active" or
                "get_cohn_status" or "create_cohn_cert" or "clear_cohn_cert" =>
                throw new NotSupportedException(
                    $"Command '{request.Name}' requires a GPSharp operation/transport that is not implemented."),
            _ => throw new NotSupportedException($"Unknown or unsupported command '{request.Name}'.")
        };
    }

    private async Task<string> SetShutterAsync(string cameraId, bool on)
    {
        await _sdk.SetShutterAsync(cameraId, on);
        return JsonSerializer.Serialize(new { success = true }, JsonOptions);
    }

    private static string RequireCameraId(string? cameraId)
    {
        if (string.IsNullOrWhiteSpace(cameraId))
        {
            throw new ArgumentException("This command requires a cameraId from list_cameras.");
        }

        return cameraId;
    }

    private async Task<string> SetSettingAsync(CameraCommand request)
    {
        var cameraId = RequireCameraId(request.CameraId);
        var settingId = RequireId(request.SettingId);
        if (request.Option is null or < 0)
            throw new ArgumentException("A non-negative integer option is required.");
        if (request.Confirm != true)
            throw new ArgumentException("Explicit confirmation is required to change a setting.");
        await _sdk.SetSettingAsync(cameraId, settingId, request.Option.Value);
        return JsonSerializer.Serialize(new { success = true }, JsonOptions);
    }

    private static int RequireId(int? id) =>
        id is >= 0 ? id.Value : throw new ArgumentException("A non-negative integer ID is required.");

    private sealed record CameraCommand(
        string Name, string? CameraId, int? SettingId, int? StatusId, long? Option, bool? Confirm);
}
