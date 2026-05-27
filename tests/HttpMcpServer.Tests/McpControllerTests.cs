using Xunit;
using HttpMcpServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Moq;
using OpenGoPro.Client;

namespace HttpMcpServer.Tests
{
    public class McpControllerTests
    {
        [Fact]
        public async Task Send_ReturnsUnknownCommand_WhenCommandNotSupported()
        {
            var mockClient = new Mock<IOpenGoProClient>();
            mockClient.Setup(c => c.SendMcpCommandAsync("unknown_cmd"))
                .ReturnsAsync("Unknown command unknown_cmd");

            var ctrl = new McpController(mockClient.Object);
            var res = await ctrl.Send(new CommandRequest { Command = "unknown_cmd" }) as OkObjectResult;
            Assert.NotNull(res);
            Assert.Equal(200, res.StatusCode);
        }

        [Fact]
        public async Task Send_ReturnsResult_WhenCommandSucceeds()
        {
            var mockClient = new Mock<IOpenGoProClient>();
            mockClient.Setup(c => c.SendMcpCommandAsync("get_last_media"))
                .ReturnsAsync("media-data");

            var ctrl = new McpController(mockClient.Object);
            var res = await ctrl.Send(new CommandRequest { Command = "get_last_media" }) as OkObjectResult;
            Assert.NotNull(res);
            Assert.Equal(200, res.StatusCode);
        }
    }
}
