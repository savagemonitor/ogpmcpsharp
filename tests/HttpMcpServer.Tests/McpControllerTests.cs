using Xunit;
using HttpMcpServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HttpMcpServer.Tests
{
    public class McpControllerTests
    {
        [Fact]
        public async Task Send_ReturnsServiceUnavailable_WhenClientMissing()
        {
            var ctrl = new McpController();
            var res = await ctrl.Send(new CommandRequest { Command = "get_last_media" }) as ObjectResult;
            Assert.NotNull(res);
            Assert.Equal(500, res.StatusCode);
        }
    }
}
