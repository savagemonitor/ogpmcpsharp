# ogpmcpsharp

This repository contains .NET 10 MCP servers for GoPro cameras using GPSharp.

## Setup and run

Initialize the pinned SDK submodule and build:

```powershell
git submodule update --init --recursive
dotnet restore
dotnet build ogpmcpsharp.slnx
dotnet test ogpmcpsharp.slnx
```

Run the HTTP server:

```powershell
dotnet run --project src\HttpMcpServer --launch-profile http
```

Connect an MCP client using **Streamable HTTP** at `http://localhost:5008/mcp`.
For clients that accept JSON configuration, use the appropriate HTTP/URL entry,
for example:

```json
{
  "mcpServers": {
    "gopro": {
      "type": "http",
      "url": "http://localhost:5008/mcp"
    }
  }
}
```

Configuration syntax varies by client. The server exposes `list_cameras`,
`list_media`, `get_camera_state`, `get_setting`, `get_status`, `set_setting`,
`shutter_start`, and `shutter_stop`. Call `list_cameras` first and
pass its camera ID as `cameraId` to the other tools. Shutter tools change camera
state and can start or stop recording or capture a photo.

`get_camera_state` returns the camera's catalogued settings and statuses keyed by
numeric Open GoPro IDs; uncatalogued IDs are omitted. `get_setting` and
`get_status` read a single ID, including an explicitly requested uncatalogued ID.
Documented Boolean statuses are returned as JSON `true` or `false` using GPSharp's
decoder; invalid Boolean values produce an error. Other values remain raw JSON.
The GPSharp camera-state API itself retains all raw values.
IDs are non-negative 32-bit integers. Values and supported options depend on the camera
and operating mode; for example, setting IDs 2 and 3 are video resolution and
frame rate, while status IDs 10 and 70 are encoding and battery percentage.
`set_setting` changes camera configuration and requires `cameraId`,
`settingId`, a non-negative integer `option`, and `confirm: true` after explicit
user approval of that exact change. A camera may reject unsupported options.
The confirmation argument is a client-provided guard, not an authorization
mechanism; clients must obtain user approval before invoking it.

The HTTP transport is stateless and has no authentication. Run it on loopback
only; do not expose it to untrusted networks or users. `AllowedHosts` defaults
to loopback names. A deployment that changes the bind address or accepted hosts
must add appropriate authentication and network protection. Discovery supports
cancellation; GPSharp's media and shutter calls do not, so a client disconnect
does not guarantee an in-flight camera operation was stopped.

The separate console program remains a legacy JSON command-line interface, not
an MCP stdio server. It accepts one command per line:

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

See [the command mapping](docs/proto_mapping.md) for limitations, including proto-only operations that have no transport yet. Media downloading is not implemented.
