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

The HTTP and console hosts share the GPSharp-backed client. Commands are JSON objects passed as text:

```json
{"name":"list_cameras"}
{"name":"list_media","cameraId":"1234"}
{"name":"shutter_start","cameraId":"1234"}
{"name":"shutter_stop","cameraId":"1234"}
```

Only discovery, media listing, and shutter start/stop are implemented in GPSharp Phase 1. See [the command mapping](docs/proto_mapping.md) for limitations, including proto-only operations that have no transport yet.
