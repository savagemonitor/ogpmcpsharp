using System;
using System.Linq;
using System.Text.Json;
using GpSharp.Data;

namespace OpenGoPro.Client;

public static class CameraStatePresentation
{
    public static object FromState(CameraState state) => new
    {
        Settings = state.Settings
            .Where(item => OpenGoProProtocolCatalog.GetSetting(item.Key) is not null)
            .ToDictionary(item => item.Key, item => item.Value),
        Statuses = state.Statuses
            .Where(item => OpenGoProProtocolCatalog.GetStatus(item.Key) is not null)
            .ToDictionary(item => item.Key, item => StatusValue(item.Key, item.Value))
    };

    public static object StatusValue(int statusId, JsonElement value)
    {
        var metadata = OpenGoProProtocolCatalog.GetStatus(statusId);
        if (metadata?.ClrType != typeof(bool))
            return value;

        if (metadata.TryGetTypedValue(value, out var decoded) && decoded is bool boolean)
            return boolean;

        throw new FormatException($"Status {statusId} is documented as Boolean but its value could not be decoded.");
    }
}
