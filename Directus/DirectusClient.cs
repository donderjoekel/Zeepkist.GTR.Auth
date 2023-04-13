using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Newtonsoft.Json;

namespace TNRD.Zeepkist.GTR.Auth.Directus
{
    internal class DirectusClient : IDirectusClient
    {
        private readonly IHttpClientFactory httpClientFactory;

        private static JsonSerializerSettings postSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public DirectusClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateClient()
        {
            return httpClientFactory.CreateClient("directus");
        }

        public async Task<Result<string>> Get(string requestUri, CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = CreateClient();
            using HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result
                .Ok(content)
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        public async Task<Result<T>> Get<T>(string requestUri, CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = CreateClient();
            using HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return Result
                    .Ok()
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            T? content;

            try
            {
                string data = await response.Content.ReadAsStringAsync(cancellationToken);
                content = JsonConvert.DeserializeObject<T>(data);
            }
            catch (JsonSerializationException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            return Result
                .Ok(content)
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        public async Task<Result<TAbstract>> Get<TAbstract, TConcrete>(
            string requestUri,
            CancellationToken cancellationToken = default
        ) where TConcrete : TAbstract
        {
            HttpClient httpClient = CreateClient();
            using HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return Result
                    .Ok()
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            TAbstract? content;

            try
            {
                string data = await response.Content.ReadAsStringAsync(cancellationToken);
                content = JsonConvert.DeserializeObject<TConcrete>(data);
            }
            catch (JsonSerializationException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            return Result
                .Ok(content)
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        /// <inheritdoc />
        public async Task<Result> Patch(string requestUri, object data, CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = CreateClient();
            string requestJson = JsonConvert.SerializeObject(data);
            using HttpResponseMessage response = await httpClient.PatchAsync(requestUri,
                new StringContent(requestJson, Encoding.UTF8, "application/json"),
                cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return Result
                    .Ok()
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            return Result
                .Ok()
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        public async Task<Result<T>> Patch<T>(
            string requestUri,
            object data,
            CancellationToken cancellationToken = default
        )
        {
            HttpClient httpClient = CreateClient();
            string requestJson = JsonConvert.SerializeObject(data);
            using HttpResponseMessage response = await httpClient.PatchAsync(requestUri,
                new StringContent(requestJson, Encoding.UTF8, "application/json"),
                cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return Result
                    .Ok()
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            T? content;

            try
            {
                string stringContent = await response.Content.ReadAsStringAsync(cancellationToken);
                content = JsonConvert.DeserializeObject<T>(stringContent);
            }
            catch (JsonSerializationException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            return Result
                .Ok(content)
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        public async Task<Result<TAbstract>> Patch<TAbstract, TConcrete>(
            string requestUri,
            object data,
            CancellationToken cancellationToken = default
        ) where TConcrete : TAbstract
        {
            HttpClient httpClient = CreateClient();
            string requestJson = JsonConvert.SerializeObject(data);
            using HttpResponseMessage response = await httpClient.PatchAsync(requestUri,
                new StringContent(requestJson, Encoding.UTF8, "application/json"),
                cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return Result
                    .Ok()
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            TAbstract? content;

            try
            {
                string stringContent = await response.Content.ReadAsStringAsync(cancellationToken);
                content = JsonConvert.DeserializeObject<TConcrete>(stringContent);
            }
            catch (JsonSerializationException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            return Result
                .Ok(content)
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        public async Task<Result> Post(string requestUri, object data, CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = CreateClient();
            string requestJson = JsonConvert.SerializeObject(data);
            using HttpResponseMessage response = await httpClient.PostAsync(requestUri,
                new StringContent(requestJson, Encoding.UTF8, "application/json"),
                cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result.Fail(new ExceptionalError(e));
            }

            return Result.Ok();
        }

        public async Task<Result<T>> Post<T>(
            string requestUri,
            object data,
            CancellationToken cancellationToken = default
        )
        {
            HttpClient httpClient = CreateClient();
            string requestJson = JsonConvert.SerializeObject(data, postSettings);
            using HttpResponseMessage response = await httpClient.PostAsync(requestUri,
                new StringContent(requestJson, Encoding.UTF8, "application/json"),
                cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result.Fail(new ExceptionalError(e));
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return default;
            }

            T? content;

            try
            {
                string stringContent = await response.Content.ReadAsStringAsync(cancellationToken);
                content = JsonConvert.DeserializeObject<T>(stringContent);
            }
            catch (JsonSerializationException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            return Result
                .Ok(content)
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        public async Task<Result<TAbstract>> Post<TAbstract, TConcrete>(
            string requestUri,
            object data,
            CancellationToken cancellationToken = default
        ) where TConcrete : TAbstract
        {
            HttpClient httpClient = CreateClient();
            string requestJson = JsonConvert.SerializeObject(data, postSettings);
            using HttpResponseMessage response = await httpClient.PostAsync(requestUri,
                new StringContent(requestJson, Encoding.UTF8, "application/json"),
                cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return Result
                    .Ok()
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            TAbstract? content;

            try
            {
                string stringContent = await response.Content.ReadAsStringAsync(cancellationToken);
                content = JsonConvert.DeserializeObject<TConcrete>(stringContent);
            }
            catch (JsonSerializationException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            return Result
                .Ok(content)
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        /// <inheritdoc />
        public async Task<Result> Delete(string requestUri, CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = CreateClient();

            using HttpResponseMessage response = await httpClient.DeleteAsync(requestUri, cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            return Result
                .Ok()
                .WithReason(new StatusCodeReason(response.StatusCode));
        }

        /// <inheritdoc />
        public async Task<Result<byte[]>> DownloadFile(string fileId, CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = CreateClient();
            using HttpResponseMessage response = await httpClient.GetAsync($"assets/{fileId}", cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Result
                    .Fail(new ExceptionalError(e))
                    .WithReason(new StatusCodeReason(response.StatusCode));
            }

            byte[] buffer = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return Result
                .Ok(buffer)
                .WithReason(new StatusCodeReason(response.StatusCode));
        }
    }
}
