using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using GpSharp.Data;

namespace OpenGoPro.Client;

public interface IGpSharpAdapter
{
    Task<IReadOnlyList<CameraSummary>> ListCamerasAsync(CancellationToken cancellationToken = default);
    Task<MediaList> GetMediaListAsync(string cameraId);
    Task SetShutterAsync(string cameraId, bool on);
    Task<CameraState> GetCameraStateAsync(string cameraId, CancellationToken cancellationToken = default);
    Task<JsonElement> GetSettingAsync(string cameraId, int settingId, CancellationToken cancellationToken = default);
    Task<JsonElement> GetStatusAsync(string cameraId, int statusId, CancellationToken cancellationToken = default);
    Task SetSettingAsync(string cameraId, int settingId, long option, CancellationToken cancellationToken = default);
}

public sealed record CameraSummary(
    string Id,
    string Name,
    string Model,
    string IpAddress,
    int Port,
    string ConnectionType,
    string? LocalLink);
