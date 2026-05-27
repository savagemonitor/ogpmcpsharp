using System.Text.Json;
using System.Threading.Tasks;

namespace OpenGoPro.Client
{
    public class OpenGoProClient : IOpenGoProClient
    {
        private readonly IProtoTransport? _transport;

        public OpenGoProClient(IProtoTransport transport)
        {
            _transport = transport;
        }

        // Parameterless constructor for simple instantiation when transport is not configured.
        public OpenGoProClient()
        {
            _transport = null;
        }

        public async Task<string> SendMcpCommandAsync(string command)
        {
            var parts = command.Split(':', 2);
            var op = parts[0];
            if (op == "get_last_media")
            {
                if (_transport == null) return "transport-not-configured";
                var res = await _transport.SendRequestAsync<object, object>("OGP_GET_LAST_MEDIA", null!);
                return JsonSerializer.Serialize(res);
            }
            else if (op == "set_camera_name")
            {
                if (_transport == null) return "transport-not-configured";
                var name = parts.Length > 1 ? parts[1] : string.Empty;
                var req = new { name = name };
                var res = await _transport.SendRequestAsync<object, object>("GPCAMERA_SET_CAMERA_NAME", req);
                return JsonSerializer.Serialize(res);
            }
            else
            {
                return $"Unknown command {command}";
            }
        }
    }
}
