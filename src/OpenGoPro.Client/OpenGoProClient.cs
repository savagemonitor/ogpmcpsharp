using System.Threading.Tasks;

namespace OpenGoPro.Client
{
    public class OpenGoProClient : IOpenGoProClient
    {
        public Task<string> SendMcpCommandAsync(string command)
        {
            // TODO: implement actual OpenGoPro integration using generated proto classes.
            return Task.FromResult($"Echo: {command}");
        }
    }
}
