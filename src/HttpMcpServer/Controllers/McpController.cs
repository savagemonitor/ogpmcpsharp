using Microsoft.AspNetCore.Mvc;
using OpenGoPro.Client;
using System.Threading.Tasks;

namespace HttpMcpServer.Controllers
{
    [ApiController]
    [Route("mcp")]
    public class McpController : ControllerBase
    {
        private readonly IOpenGoProClient _client;

        public McpController(IOpenGoProClient client)
        {
            _client = client;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] CommandRequest req)
        {
            var result = await _client.SendMcpCommandAsync(req.Command);
            return Ok(new { result = result });
        }
    }

    public class CommandRequest { public required string Command { get; set; } }
}
