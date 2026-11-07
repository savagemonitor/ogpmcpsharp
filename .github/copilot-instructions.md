# Copilot instructions for ogpmcpsharp

## Build, test, and run

Initialize the pinned GPSharp submodule before restoring:

```powershell
git submodule update --init --recursive
dotnet restore
dotnet build ogpmcpsharp.slnx
dotnet test ogpmcpsharp.slnx
```

Run the servers locally:

```powershell
dotnet run --project src\HttpMcpServer
dotnet run --project src\ConsoleMcpServer
```

The shared client is in `src\OpenGoPro.Client`; both hosts use its GPSharp adapter. MCP command behavior and supported operations are documented in `docs\proto_mapping.md`.

## GPSharp dependency

- GPSharp is pinned as a Git submodule at `third_party\gpsharp`; its `src\GpSharp\GpSharp.csproj` is referenced using a relative project reference.
- The submodule gitlink is the version pin. Updating GPSharp requires intentionally changing that gitlink and validating the MCP solution.
- `Google.Protobuf` and proto generation are owned by GPSharp; do not re-add direct generated-proto wiring to the MCP client.
- GPSharp protobuf messages do not imply an implemented camera operation/transport. Only expose SDK operations that exist and surface unsupported functionality explicitly.
- Preserve `third_party\gpsharp\src\GpSharp\Protos\OpenGoPro\NOTICE.md` and the upstream schema copyright/provenance information.

## Architecture

- `src\OpenGoPro.Client`: shared MCP-to-GPSharp adapter and command protocol.
- `src\HttpMcpServer`: ASP.NET host exposing `POST /mcp/send`.
- `src\ConsoleMcpServer`: console host using the same shared client.
- `tests\*`: xUnit tests (Moq is used for adapter/host fakes).
- `docs\proto_mapping.md`: canonical command names, payloads, and support status.

Supported Phase 1 commands are camera discovery, media listing, and shutter start/stop. Settings/status operations and protobuf-only operations without GPSharp transports must remain explicit errors until implemented by the SDK.

## Security advisories

When a vulnerability or CVE is discovered in a dependency or code, promptly assess applicability and severity and address it with a supported fix or mitigation; if blocked, document the concrete blocker and safe interim mitigation. Do not silently ignore or defer known vulnerabilities. Never expose secrets, credentials, tokens, or sensitive user/device data in logs, tests, issues, or reports.
