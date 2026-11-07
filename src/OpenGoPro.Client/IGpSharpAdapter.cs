using System.Collections.Generic;
using System.Threading.Tasks;
using GpSharp.Data;

namespace OpenGoPro.Client;

public interface IGpSharpAdapter
{
    Task<IReadOnlyList<CameraSummary>> ListCamerasAsync();
    Task<MediaList> GetMediaListAsync(string cameraId);
    Task SetShutterAsync(string cameraId, bool on);
}

public sealed record CameraSummary(
    string Id,
    string Name,
    string Model,
    string IpAddress,
    int Port,
    string ConnectionType,
    string? LocalLink);
