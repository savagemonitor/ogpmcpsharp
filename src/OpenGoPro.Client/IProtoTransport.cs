using System.Threading;
using System.Threading.Tasks;

namespace OpenGoPro.Client
{
    public interface IProtoTransport
    {
        Task<TResponse> SendRequestAsync<TRequest, TResponse>(string operationId, TRequest request, CancellationToken cancellationToken = default);
    }
}
