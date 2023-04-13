using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace TNRD.Zeepkist.GTR.Auth.Directus;

public interface IDirectusClient
{
    Task<Result<string>> Get(string requestUri, CancellationToken cancellationToken = default);
    Task<Result<T>> Get<T>(string requestUri, CancellationToken cancellationToken = default);

    Task<Result<TAbstract>> Get<TAbstract, TConcrete>(
        string requestUri,
        CancellationToken cancellationToken = default
    ) where TConcrete : TAbstract;

    Task<Result> Patch(string requestUri, object data, CancellationToken cancellationToken = default);

    Task<Result<T>> Patch<T>(string requestUri, object data, CancellationToken cancellationToken = default);

    Task<Result<TAbstract>> Patch<TAbstract, TConcrete>(
        string requestUri,
        object data,
        CancellationToken cancellationToken = default
    ) where TConcrete : TAbstract;

    Task<Result> Post(string requestUri, object data, CancellationToken cancellationToken = default);

    Task<Result<T>> Post<T>(
        string requestUri,
        object data,
        CancellationToken cancellationToken = default
    );

    Task<Result<TAbstract>> Post<TAbstract, TConcrete>(
        string requestUri,
        object data,
        CancellationToken cancellationToken = default
    ) where TConcrete : TAbstract;

    Task<Result> Delete(string requestUri, CancellationToken cancellationToken = default);

    Task<Result<byte[]>> DownloadFile(string fileId, CancellationToken cancellationToken = default);
}
