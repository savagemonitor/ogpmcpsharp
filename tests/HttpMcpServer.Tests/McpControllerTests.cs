using Moq;
using OpenGoPro.Client;
using HttpMcpServer.Controllers;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace HttpMcpServer.Tests
{
    public class McpControllerTests
    {
        [Fact]
        public async Task Send_ReturnsOk()
        {
            var mock = new Mock<IOpenGoProClient>();
            mock.Setup(m => m.SendMcpCommandAsync("cmd")).ReturnsAsync("ok");
            var ctrl = new McpController(mock.Object);
            var res = await ctrl.Send(new CommandRequest { Command = "cmd" }) as OkObjectResult;
            Assert.NotNull(res);
        }

        [Fact]
        public async Task Send_UnsupportedCommand_ReturnsNotImplemented()
        {
            var mock = new Mock<IOpenGoProClient>();
            mock.Setup(m => m.SendMcpCommandAsync("cmd"))
                .ThrowsAsync(new NotSupportedException("Unsupported command."));
            var ctrl = new McpController(mock.Object);

            var result = await ctrl.Send(new CommandRequest { Command = "cmd" });

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(501, status.StatusCode);
        }
    }
}
