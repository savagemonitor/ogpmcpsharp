# MCP command mapping and GPSharp support

The shared `OpenGoProClient` is the adapter used by both the HTTP and console hosts. Commands are passed as JSON text in the existing `command` field (or as one JSON line in the console):

```json
{"name":"list_cameras"}
{"name":"list_media","cameraId":"1234"}
{"name":"shutter_start","cameraId":"1234"}
{"name":"shutter_stop","cameraId":"1234"}
```

`cameraId` is the `DiscoveredDevice.Id.PartialSerial` returned by `list_cameras`. Discovery runs for up to five seconds. GPSharp may report multiple network endpoints for the same camera; the adapter returns one endpoint per camera ID. Run discovery before a camera-specific command. Camera operations connect lazily and use GPSharp's HTTP transport.

| MCP command | GPSharp API | Status |
|---|---|---|
| `list_cameras` | `OgpSdk.DiscoverAsync` | Implemented; returns camera ID/name/model/address/port and discovery connection/link evidence. |
| `list_media` | `GoPro.Commands.GetMediaListAsync` | Implemented; returns the parsed GPSharp media list. Requires a discovered camera ID. |
| `shutter_start` / `shutter_stop` | `GoPro.Commands.SetShutterAsync(true/false)` | Implemented; requires a discovered camera ID. |
| `set_camera_name` | `RequestSetCameraName` protobuf exists in GPSharp | Unsupported: no GPSharp operation/transport is implemented for it. |
| `get_last_media` | `RequestGetLastCapturedMedia` protobuf exists in GPSharp | Unsupported: message availability is not an operation or transport. |
| `set_turbo_active` | `RequestSetTurboActive` protobuf exists in GPSharp | Unsupported: no GPSharp operation/transport is implemented. |
| `get_cohn_status`, `create_cohn_cert`, `clear_cohn_cert` | COHN protobuf messages exist in GPSharp | Unsupported: no GPSharp operation/transport is implemented. |
| Camera setting/status reads | GPSharp Settings/Statuses interfaces | Unsupported in Phase 1; GPSharp throws `NotSupportedException` because BLE support is not implemented. |

GPSharp protobufs are serialization types only; they do not define a gRPC service or send commands to the camera. Unsupported commands are surfaced as errors (HTTP 501, console error output), not success-shaped placeholders.

## Dependency and schema provenance

The MCP shared client references GPSharp from the pinned `third_party/gpsharp` submodule using a relative project reference. GPSharp contains the OpenGoPro schemas and the `GpSharp.Protos.OpenGoPro` generated types. `third_party/opengopro-protos` is no longer needed by the MCP build. GPSharp's `src/GpSharp/Protos/OpenGoPro/NOTICE.md` records upstream schema provenance and attribution; retain that notice with the SDK.

Initialize nested submodules after cloning:

```powershell
git submodule update --init --recursive
dotnet restore
dotnet build ogpmcpsharp.slnx
dotnet test ogpmcpsharp.slnx
```

The SDK submodule gitlink pins the exact GPSharp source revision. SDK updates must intentionally change that pin and pass the MCP solution build/tests. When an official package/version is available, the project reference can be replaced with an exact package version.
