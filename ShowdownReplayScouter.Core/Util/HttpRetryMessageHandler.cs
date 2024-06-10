using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace ShowdownReplayScouter.Core.Util
{
    // From https://stackoverflow.com/a/35183487
    public class HttpRetryMessageHandler(HttpClientHandler handler) : DelegatingHandler(handler)
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .OrResult<HttpResponseMessage>(x =>
                    !x.IsSuccessStatusCode && x.StatusCode != System.Net.HttpStatusCode.NotFound
                )
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt))
                )
                .ExecuteAsync(() => base.SendAsync(request, cancellationToken));
    }
}
