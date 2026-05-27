# Copilot instructions for ogpmcpsharp

Purpose
- Help future Copilot sessions and contributors understand how to build, test, and work with this repo quickly.

Build, test, and run (commands)
- Restore & build solution:
  dotnet restore
  dotnet build ogpmcpsharp.slnx

- Run all tests:
  dotnet test ogpmcpsharp.slnx

- Run a single test project:
  dotnet test src\HttpMcpServer\HttpMcpServer.csproj

- Run a single test method (example):
  dotnet test --filter "FullyQualifiedName~Namespace.ClassName.TestMethod"
  or by DisplayName:
  dotnet test --filter "DisplayName~TestName"

- Run servers locally:
  dotnet run --project src\HttpMcpServer
  dotnet run --project src\ConsoleMcpServer

- Submodule (protos) setup:
  git submodule update --init --recursive

Where generated protos come from
- Protobuf files live in the submodule: third_party/opengopro-protos/protobuf/**.proto
- C# types are generated at build via Grpc.Tools configured in src/OpenGoPro.Client/OpenGoPro.Client.csproj
- If you inspect runtime issues, ensure CopyLocalLockFileAssemblies is honored in OpenGoPro.Client to keep Google.Protobuf and other binaries available.

High-level architecture
- Solution: ogpmcpsharp.slnx
- Projects:
  - src/OpenGoPro.Client: shared library. Contains generated proto types, IProtoTransport abstraction, OpenApiHttpTransport, and OpenGoProClient (high-level client that maps MCP commands to transports). Also references the ModelContextProtocol C# SDK.
  - src/HttpMcpServer: ASP.NET minimal/API server exposing MCP over HTTP; temporary controller wiring uses reflection to invoke OpenGoProClient.
  - src/ConsoleMcpServer: console-hosted MCP server (CLI-style).
  - tests/*: xUnit test projects for each project (xUnit + Moq used).
- Transport model:
  - IProtoTransport: abstraction for sending proto request/response messages (proto-backed transport not implemented yet).
  - OpenApiHttpTransport: reads an OpenAPI JSON (operationId -> method/path) and maps client calls to camera HTTP endpoints.
- Mapping:
  - docs/proto_mapping.md is the canonical mapping of MCP commands → proto messages and HTTP operations (OpenAPI-derived). Connection-establishment mappings were moved to internal client/transport responsibilities.

Key conventions and repository-specific patterns
- OpenAPI integration:
  - Environment variables used at runtime: OGP_OPENAPI_PATH (default used in dev: C:\Users\savag\Downloads\openapi.json), OGP_CAMERA_BASE (default http://127.0.0.1/).
  - OpenApiHttpTransport expects operationId values in the OpenAPI document and maps them to request building logic (path template filling, query parameters, JSON body for POST/PUT/PATCH).

- Protos as a git submodule:
  - The repo uses a git submodule (third_party/opengopro-protos). Always run git submodule update --init --recursive after cloning.

- Mapping file is authoritative:
  - docs/proto_mapping.md is the single source-of-truth for which MCP commands map to which proto messages or HTTP operations. Update it when adding commands or OpenAPI-derived mappings.

- Temporary reflection-based controller:
  - HttpMcpServer currently invokes OpenGoProClient via reflection to avoid tight compile-time coupling; expect DI refactor later. Tests may rely on this behavior.

- Tests & mocking:
  - xUnit + Moq. When adding tests that exercise HTTP logic, prefer injecting a mocked HttpMessageHandler into HttpClient used by OpenApiHttpTransport to avoid network calls.

- ModelContextProtocol SDK:
  - The C# SDK (ModelContextProtocol) is used in OpenGoPro.Client — keep package references in sync when upgrading.

Files and places to look first
- docs/proto_mapping.md — canonical mapping and OpenAPI-derived mappings
- src/OpenGoPro.Client — client, transports, proto wiring
- src/OpenGoPro.Client/OpenGoPro.Client.csproj — Protobuf Include and generation settings
- src/OpenGoPro.Client/OpenApiHttpTransport.cs — OpenAPI-driven HTTP transport
- src/HttpMcpServer — HTTP server and controller

Other AI assistant configs
- No repository-specific assistant rules (CLAUDE.md, .cursorrules, AGENTS.md, .windsurfrules, CONVENTIONS.md) were found when this file was generated.

Notes for Copilot sessions
- Before making changes that affect the proto-generated classes, ensure submodule is initialized and built so generated types exist.
- Prefer adding unit tests that mock IProtoTransport and OpenApiHttpTransport rather than hitting real cameras.
- Update docs/proto_mapping.md whenever adding or removing exposed MCP operations; Copilot agents should consult that file first when modifying command mappings.

MCP server configuration
- If you want, I can add dev-time run configurations or launch profiles for the HttpMcpServer (e.g., a simple VSCode launch configuration or a small Playwright-like test harness). Tell me which server you'd like configured and any preferred ports or endpoints.

Summary
- Added concise commands, architecture overview, and repo-specific conventions to help future Copilot sessions and contributors onboard quickly.

