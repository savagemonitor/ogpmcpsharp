using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace HttpMcpServer.Controllers
{
    [ApiController]
    [Route("mcp")]
    public class McpController : ControllerBase
    {
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] CommandRequest req)
        {
            // Try to find OpenGoPro.Client.OpenGoProClient via loaded assemblies
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "OpenGoPro.Client");
            Type clientType = null;
            object client = null;
            if (asm != null) clientType = asm.GetType("OpenGoPro.Client.OpenGoProClient");
            if (clientType != null)
            {
                try { client = Activator.CreateInstance(clientType); }
                catch { client = null; }
            }

            if (client == null)
            {
                return StatusCode(500, new { error = "OpenGoPro client not available" });
            }

            var method = clientType.GetMethod("SendMcpCommandAsync");
            var taskObj = method.Invoke(client, new object[] { req.Command }) as System.Threading.Tasks.Task;
            if (taskObj == null) return StatusCode(500, new { error = "Failed to invoke client method" });
            await taskObj.ConfigureAwait(false);
            var resultProp = taskObj.GetType().GetProperty("Result");
            var result = resultProp.GetValue(taskObj) as string;
            return Ok(new { result = result });
        }
    }

    public class CommandRequest { public string Command { get; set; } }
}
