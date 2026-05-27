using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using OpenGoPro.Client;

namespace HttpMcpServer.Controllers
{
    [ApiController]
    [Route("mcp")]
    public class McpController : ControllerBase
    {
        private readonly IOpenGoProClient _client;
        public McpController(IOpenGoProClient client) => _client = client;

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] CommandRequest req)
        {
            var res = await _client.SendMcpCommandAsync(req.Command);
            return Ok(new { result = res });
        }
    }

    public class CommandRequest { public string Command { get; set; } }
}
