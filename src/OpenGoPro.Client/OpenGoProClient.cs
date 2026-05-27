using System.Text.Json;
using System.Threading.Tasks;
using OpenGoPro.Client.Generated;

namespace OpenGoPro.Client
{
    public class OpenGoProClient : IOpenGoProClient
    {
        private readonly IProtoTransport? _transport;
        private readonly IOpenGoProHttpClient? _httpClient;

        public OpenGoProClient(IProtoTransport? transport, IOpenGoProHttpClient? httpClient)
        {
            _transport = transport;
            _httpClient = httpClient;
        }

        public OpenGoProClient(IProtoTransport? transport) : this(transport, null)
        {
        }

        public OpenGoProClient(IOpenGoProHttpClient? httpClient) : this(null, httpClient)
        {
        }

        // Parameterless constructor for simple instantiation when transport is not configured.
        public OpenGoProClient()
        {
            _transport = null;
            _httpClient = null;
        }

        public async Task<string> SendMcpCommandAsync(string command)
        {
            var parts = command.Split(':', 2);
            var op = parts[0];

            if (op == "get_last_media")
            {
                // Prefer proto transport when available
                if (_transport != null)
                {
                    var res = await _transport.SendRequestAsync<object, object>("OGP_GET_LAST_MEDIA", null!);
                    return JsonSerializer.Serialize(res);
                }
                if (_httpClient != null)
                {
                    var res = await _httpClient.GetLastMediaAsync();
                    return JsonSerializer.Serialize(res);
                }
                return "transport-not-configured";
            }
            else if (op == "set_camera_name")
            {
                var name = parts.Length > 1 ? parts[1] : string.Empty;
                if (_transport != null)
                {
                    var req = new { name = name };
                    var res = await _transport.SendRequestAsync<object, object>("GPCAMERA_SET_CAMERA_NAME", req);
                    return JsonSerializer.Serialize(res);
                }
                if (_httpClient != null)
                {
                    var res = await _httpClient.SetCameraNameAsync(name);
                    return res;
                }
                return "transport-not-configured";
            }
            else
            {
                return $"Unknown command {command}";
            }
        }
    }
}
