MCP → OpenGoPro proto mapping

Overview
This file maps high-level MCP command names (used by the MCP server) to OpenGoPro protobuf request messages found in third_party/opengopro-protos/protobuf.

Mapping (initial sensible defaults)

- list_cameras
  - Proto: (no dedicated proto) — handled by discovery/transport layer; returns list of available cameras (out of scope for protos)

- set_camera_name
  - Proto: RequestSetCameraName (camera_control.proto)
  - Description: Set the camera's display name; maps MCP payload "name" → RequestSetCameraName.name

- connect_ap
  - Proto: RequestConnect (network_management.proto)
  - Description: Connect to a previously-provisioned AP. MCP payload: { "ssid": "..." }

- connect_ap_new
  - Proto: RequestConnectNew (network_management.proto)
  - Description: Connect to and authenticate to a new AP. MCP payload: { "ssid":"...", "password":"..." }

- start_scan
  - Proto: RequestStartScan (network_management.proto)
  - Description: Instruct camera to scan for APs. No payload required.

- get_scan_results
  - Proto: RequestGetApEntries (network_management.proto)
  - Description: Retrieve AP scan results. MCP payload: { "scan_id": <int>, "start_index": 0, "max_entries": 50 }

- release_network
  - Proto: RequestReleaseNetwork (network_management.proto)
  - Description: Disconnect from current AP and return to AP mode.

- pair_finish
  - Proto: RequestPairingFinish (network_management.proto)
  - Description: Mark pairing finished. MCP payload: { "result": 0|1, "phoneName": "..." }

- get_last_media
  - Proto: RequestGetLastCapturedMedia (media.proto)
  - Description: Retrieve metadata/path of last captured media file.

- set_turbo_active
  - Proto: RequestSetTurboActive (turbo_transfer.proto)
  - Description: Enable/disable turbo transfer. MCP payload: { "active": true }

- get_cohn_status
  - Proto: RequestGetCOHNStatus (cohn.proto)
  - Description: Request Camera On the Home Network status; optional register flag: MCP payload { "register": true }

- create_cohn_cert
  - Proto: RequestCreateCOHNCert (cohn.proto)
  - Description: Create COHN cert; MCP payload: { "override": true }

- clear_cohn_cert
  - Proto: RequestClearCOHNCert (cohn.proto)
  - Description: Clear COHN cert; no payload.

Notes and next steps
- The OpenGoPro protos define request/response messages but not gRPC service definitions; the client will need a transport layer (e.g., TCP/HTTP/WebSocket or MCU-specific transport) that serializes protobuf messages and sends them to the camera.
- Implementation plan:
  1. Build generated C# types from protos (already wired into OpenGoPro.Client project via Protobuf Include). Verify generated types appear in src/OpenGoPro.Client/obj or bin output after build.
  2. Implement a transport abstraction IProtoTransport with methods SendRequestAsync<TRequest, TResponse>(TRequest) that serializes the request and receives/parses the response message type.
  3. Implement OpenGoProClient to map MCP commands to proto requests (using mapping above), call the transport, and return MCP-compatible responses.
  4. Add unit tests that mock IProtoTransport to assert correct proto message creation and response handling.

If any of these mappings should be changed or you want additional MCP commands mapped, say which ones and preferred names.
