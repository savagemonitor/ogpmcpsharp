using System.Threading.Tasks;

namespace OpenGoPro.Client
{
    public interface IOpenGoProClient
    {
        Task<string> SendMcpCommandAsync(string command);
    }
}
