using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GpSharp.Core;
using GpSharp.Data;
using GpSharp.GoPro;
using GpSharp.Network;

namespace OpenGoPro.Client;

public sealed class GpSharpAdapter : IGpSharpAdapter
{
    private static readonly TimeSpan DiscoveryWindow = TimeSpan.FromSeconds(5);
    private readonly OgpSdk _sdk;
    private readonly ConcurrentDictionary<string, DiscoveredDevice> _devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, GoPro> _cameras = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    public GpSharpAdapter(OgpSdk sdk)
    {
        _sdk = sdk;
    }

    public async Task<IReadOnlyList<CameraSummary>> ListCamerasAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(DiscoveryWindow);
        var discovered = new Dictionary<string, DiscoveredDevice>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await foreach (var device in _sdk.DiscoverAsync(timeout.Token).WithCancellation(timeout.Token))
            {
                var id = device.Id.PartialSerial;
                discovered.TryAdd(id, device);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
        }

        foreach (var (id, device) in discovered)
        {
            _devices[id] = device;
        }

        return discovered.Values
            .Select(device => new CameraSummary(
                device.Id.PartialSerial,
                device.Name,
                device.Model,
                device.IpAddress,
                device.Port,
                device.ConnectionType.ToString(),
                device.DiscoveryInterface?.LocalLink.ToString()))
            .ToArray();
    }

    public async Task<MediaList> GetMediaListAsync(string cameraId)
    {
        var camera = await GetCameraAsync(cameraId);
        var result = await camera.Commands.GetMediaListAsync();
        if (!result.IsSuccess)
        {
            throw result.Error ?? new InvalidOperationException("GPSharp failed to retrieve the media list.");
        }

        return result.Value
            ?? throw new InvalidOperationException("GPSharp returned a successful media result without a value.");
    }

    public async Task SetShutterAsync(string cameraId, bool on)
    {
        var camera = await GetCameraAsync(cameraId);
        var result = await camera.Commands.SetShutterAsync(on);
        if (!result.IsSuccess)
        {
            throw result.Error ?? new InvalidOperationException("GPSharp failed to change shutter state.");
        }
    }

    private async Task<GoPro> GetCameraAsync(string cameraId)
    {
        if (_cameras.TryGetValue(cameraId, out var camera))
        {
            return camera;
        }

        await _connectLock.WaitAsync();
        try
        {
            if (_cameras.TryGetValue(cameraId, out camera))
            {
                return camera;
            }

            if (!_devices.TryGetValue(cameraId, out var device))
            {
                throw new InvalidOperationException(
                    $"Camera '{cameraId}' is not in the current discovery list. Run list_cameras first.");
            }

            camera = await _sdk.ConnectAsync(device);
            return _cameras.GetOrAdd(cameraId, camera);
        }
        finally
        {
            _connectLock.Release();
        }
    }
}
