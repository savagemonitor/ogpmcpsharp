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
`list_media`, `shutter_start`, and `shutter_stop`. Call `list_cameras` first and
pass its camera ID as `cameraId` to the other tools. Shutter tools change camera
state and can start or stop recording or capture a photo.

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
{"name":"shutter_start","cameraId":"1234"}
{"name":"shutter_stop","cameraId":"1234"}
```

Only discovery, media listing, and shutter start/stop are implemented in GPSharp Phase 1. See [the command mapping](docs/proto_mapping.md) for limitations, including proto-only operations that have no transport yet.
