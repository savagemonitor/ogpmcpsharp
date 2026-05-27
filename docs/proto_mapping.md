MCP → OpenGoPro proto mapping

Overview
This file maps high-level MCP command names (used by the MCP server) to OpenGoPro protobuf request messages found in third_party/opengopro-protos/protobuf.

Mapping (initial sensible defaults)

- list_cameras
  - Proto: (no dedicated proto) — handled by discovery/transport layer; returns list of available cameras (out of scope for protos)

- set_camera_name
  - Proto: RequestSetCameraName (camera_control.proto)
  - Description: Set the camera's display name; maps MCP payload "name" → RequestSetCameraName.name

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

HTTP endpoint mappings (from provided OpenAPI document)

- GET /gopro/camera/name
  - OperationId: GPCAMERA_GET_CAMERA_NAME
  - Maps to: get_camera_name (returns current display name)

- PUT /gopro/camera/name
  - OperationId: GPCAMERA_SET_CAMERA_NAME
  - Maps to: set_camera_name (alternate HTTP transport)

- GET /gopro/camera/info
  - OperationId: OGP_CAMERA_INFO
  - Maps to: camera_info (camera metadata and capabilities)

- GET /gopro/camera/state
  - OperationId: OGP_GET_STATE
  - Maps to: get_state (current camera operational state)

- GET /gopro/camera/stream/start
  - OperationId: OGP_PREVIEW_STREAM_START
  - Maps to: start_preview_stream

- GET /gopro/camera/stream/stop
  - OperationId: OGP_PREVIEW_STREAM_STOP
  - Maps to: stop_preview_stream

- GET /gopro/media/last_captured
  - OperationId: OGP_GET_LAST_MEDIA
  - Maps to: get_last_media (HTTP transport)

- GET /gopro/media/list
  - OperationId: OGP_MEDIA_LIST
  - Maps to: list_media

- GET /gopro/media/thumbnail
  - OperationId: OGP_MEDIA_THUMBNAIL
  - Maps to: media_thumbnail

- GET /gopro/media/telemetry
  - OperationId: OGP_MEDIA_TELEMETRY
  - Maps to: media_telemetry

- GET /gopro/media/info
  - OperationId: OGP_MEDIA_INFO
  - Maps to: media_info

- GET /gopro/media/last_captured
  - OperationId: OGP_GET_LAST_MEDIA
  - Maps to: get_last_media

- POST /gp/gpSoftUpdate
  - OperationId: GPCAMERA_FIRMWARE_UPDATE_V2
  - Maps to: firmware_update

- GET /videos/DCIM/{directory}/{filename}
  - OperationId: OGP_DOWNLOAD_MEDIA
  - Maps to: download_media (serves media files)

Notes
- Connection-establishment mappings (connect_ap, connect_ap_new, start_scan, get_scan_results, release_network, pair_finish) were removed from top-level MCP mappings and will be implemented as internal client transport/connection management functions.
- The HTTP mappings above are additive: the OpenGoProClient will prefer proto-backed transport when available, and fall back to the HTTP endpoints (using the OpenAPI) where appropriate.
- Next steps: implement an HTTP transport that uses the OpenAPI document (C:\Users\savag\Downloads\openapi.json) to call camera endpoints; implement IProtoTransport for protobuf transport; wire OpenGoProClient to choose transport based on availability.

Notes and next steps
- The OpenGoPro protos define request/response messages but not gRPC service definitions; the client will need a transport layer (e.g., TCP/HTTP/WebSocket or MCU-specific transport) that serializes protobuf messages and sends them to the camera.
- Implementation plan:
  1. Build generated C# types from protos (already wired into OpenGoPro.Client project via Protobuf Include). Verify generated types appear in src/OpenGoPro.Client/obj or bin output after build.
  2. Implement a transport abstraction IProtoTransport with methods SendRequestAsync<TRequest, TResponse>(TRequest) that serializes the request and receives/parses the response message type.
  3. Implement OpenGoProClient to map MCP commands to proto requests (using mapping above), call the transport, and return MCP-compatible responses.
  4. Add unit tests that mock IProtoTransport to assert correct proto message creation and response handling.

If any of these mappings should be changed or you want additional MCP commands mapped, say which ones and preferred names.
