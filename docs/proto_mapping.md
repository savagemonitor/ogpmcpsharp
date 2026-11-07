# MCP command mapping and GPSharp support

The HTTP server exposes MCP tools over Streamable HTTP at `/mcp`, backed by the
shared `IGpSharpAdapter`. MCP clients pass tool arguments as JSON objects.
The console host uses the shared `OpenGoProClient` JSON dispatcher and accepts
one JSON command per line:

```json
{"name":"list_cameras"}
{"name":"list_media","cameraId":"1234"}
{"name":"get_camera_state","cameraId":"1234"}
{"name":"get_setting","cameraId":"1234","settingId":2}
{"name":"get_status","cameraId":"1234","statusId":70}
{"name":"set_setting","cameraId":"1234","settingId":2,"option":9,"confirm":true}
{"name":"shutter_start","cameraId":"1234"}
{"name":"shutter_stop","cameraId":"1234"}
```

`cameraId` is the `DiscoveredDevice.Id.PartialSerial` returned by `list_cameras`. Discovery runs for up to five seconds. GPSharp may report multiple network endpoints for the same camera; the adapter returns one endpoint per camera ID. Run discovery before a camera-specific command. Camera operations connect lazily and use GPSharp's HTTP transport.

| MCP command | GPSharp API | Status |
|---|---|---|
| `list_cameras` | `OgpSdk.DiscoverAsync` | Implemented; returns camera ID/name/model/address/port and discovery connection/link evidence. |
| `list_media` | `GoPro.Commands.GetMediaListAsync` | Implemented; returns the parsed GPSharp media list. Requires a discovered camera ID. |
| `shutter_start` / `shutter_stop` | `GoPro.Commands.SetShutterAsync(true/false)` | Implemented; requires a discovered camera ID. |
| `get_camera_state` | `GoPro.Statuses.GetCameraStateAsync` | Reads a snapshot of settings and statuses keyed by numeric IDs. |
| `get_setting` / `get_status` | `GetSetting(id).GetValueAsync<JsonElement>` / `GetStatus(id).GetValueAsync<JsonElement>` | Reads a setting or status ID from 0 to 255; status uses a fresh snapshot. |
| `set_setting` | `GetSetting(id).SetValueAsync(option)` | Changes configuration; non-negative integer option and explicit user confirmation required. Camera determines which values are supported. |
| `set_camera_name` | `RequestSetCameraName` protobuf exists in GPSharp | Unsupported: no GPSharp operation/transport is implemented for it. |
| `get_last_media` | `RequestGetLastCapturedMedia` protobuf exists in GPSharp | Unsupported: message availability is not an operation or transport. |
| `set_turbo_active` | `RequestSetTurboActive` protobuf exists in GPSharp | Unsupported: no GPSharp operation/transport is implemented. |
| `get_cohn_status`, `create_cohn_cert`, `clear_cohn_cert` | COHN protobuf messages exist in GPSharp | Unsupported: no GPSharp operation/transport is implemented. |

GPSharp protobufs are serialization types only; they do not define a gRPC service or send commands to the camera. Unsupported tools are not registered by the HTTP server; unknown MCP tool calls and unsupported console commands return errors, not success-shaped placeholders. The former custom HTTP endpoint `/mcp/send` has been removed.

The HTTP server uses stateless MCP transport; no session ID or per-client
discovery cache is provided. Discovery is shared by the process and accepts
cancellation. GPSharp's media and shutter operations cannot currently be
cancelled once started, including when a client disconnects. The shutter tools
change camera state and may capture media or start/stop recording.
Settings and status operations accept cancellation, but cancellation of a
setting write does not prove that the camera did not apply it. Setting and
status IDs and option values are model-dependent; no capability discovery or
live update stream is provided. `set_setting` requires `confirm: true` after
the user confirms the exact change. This flag is not an authorization boundary.
Media download remains unsupported.

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
