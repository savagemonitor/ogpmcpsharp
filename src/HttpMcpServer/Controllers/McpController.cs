using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
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
            try
            {
                var res = await _client.SendMcpCommandAsync(req.Command);
                return Ok(new { result = res });
            }
            catch (ArgumentException error)
            {
                return BadRequest(new { error = error.Message });
            }
            catch (NotSupportedException error)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, new { error = error.Message });
            }
            catch (InvalidOperationException error)
            {
                return NotFound(new { error = error.Message });
            }
            catch (HttpRequestException error)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = error.Message });
            }
            catch (System.Text.Json.JsonException error)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = error.Message });
            }
        }
    }

    public class CommandRequest { public string Command { get; set; } = string.Empty; }
}
