using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace ShowdownReplayScouter.Core.Util
{
    /// <summary>
    /// Retries requests that failed for a transient reason (network errors, timeouts, 408, 429 and 5xx).
    /// </summary>
    public class HttpRetryMessageHandler : DelegatingHandler
    {
        public const int RetryCount = 3;

        /// <summary>
        /// Longest wait requested by a "Retry-After" header that is honored.
        /// </summary>
        private static readonly TimeSpan MaximumRetryAfter = TimeSpan.FromMinutes(1);

        private readonly Func<int, TimeSpan> _retryDelay;

        public HttpRetryMessageHandler(HttpMessageHandler handler)
            : this(handler, null) { }

        /// <param name="handler">The handler sending the requests.</param>
        /// <param name="retryDelay">The delay before the given retry attempt (default 3s, 9s, 27s).</param>
        public HttpRetryMessageHandler(HttpMessageHandler handler, Func<int, TimeSpan>? retryDelay)
            : base(handler)
        {
            _retryDelay = retryDelay ?? ((attempt) => TimeSpan.FromSeconds(Math.Pow(3, attempt)));
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Policy
                .Handle<HttpRequestException>()
                // Timeouts, but not a cancellation requested by the caller
                .Or<TaskCanceledException>((_) => !cancellationToken.IsCancellationRequested)
                .OrResult<HttpResponseMessage>(IsTransientFailure)
                .WaitAndRetryAsync(
                    RetryCount,
                    (attempt, outcome, _) => RetryAfter(outcome.Result) ?? _retryDelay(attempt),
                    (outcome, _, _, _) =>
                    {
                        // The failed response is replaced by the retried one
                        outcome.Result?.Dispose();
                        return Task.CompletedTask;
                    }
                )
                .ExecuteAsync((token) => base.SendAsync(request, token), cancellationToken);

        private static bool IsTransientFailure(HttpResponseMessage response)
        {
            return response.StatusCode
                    is HttpStatusCode.RequestTimeout
                        or HttpStatusCode.TooManyRequests
                || (int)response.StatusCode >= 500;
        }

        private static TimeSpan? RetryAfter(HttpResponseMessage? response)
        {
            var retryAfter = response?.Headers.RetryAfter;
            var delay = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
            if (delay is null || delay < TimeSpan.Zero || delay > MaximumRetryAfter)
            {
                return null;
            }
            return delay;
        }
    }
}
