using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Client
{
    public interface IHttpMouseClientFactory
    {
        Task<IHttpMouseClient> CreateAsync(CancellationToken cancellation);
    }
}
