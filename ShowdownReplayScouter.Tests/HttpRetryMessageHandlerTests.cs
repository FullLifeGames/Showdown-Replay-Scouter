#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Tests
{
    [TestFixture]
    public class HttpRetryMessageHandlerTests
    {
        [Test]
        public async Task TransientFailuresAreRetried()
        {
            var handler = new QueuedHandler(
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.TooManyRequests,
                HttpStatusCode.OK
            );

            var response = await Send(handler).ConfigureAwait(false);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(handler.Calls, Is.EqualTo(3));
        }

        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Forbidden)]
        public async Task ClientErrorsAreNotRetried(HttpStatusCode statusCode)
        {
            var handler = new QueuedHandler(statusCode, HttpStatusCode.OK);

            var response = await Send(handler).ConfigureAwait(false);

            Assert.That(response.StatusCode, Is.EqualTo(statusCode));
            Assert.That(handler.Calls, Is.EqualTo(1));
        }

        [Test]
        public async Task PersistentFailuresReturnTheLastResponse()
        {
            var handler = new QueuedHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway
            );

            var response = await Send(handler).ConfigureAwait(false);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
            Assert.That(handler.Calls, Is.EqualTo(1 + HttpRetryMessageHandler.RetryCount));
        }

        [Test]
        public async Task NetworkErrorsAreRetried()
        {
            var handler = new QueuedHandler(HttpStatusCode.OK) { FailingCalls = 2 };

            var response = await Send(handler).ConfigureAwait(false);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(handler.Calls, Is.EqualTo(3));
        }

        [Test]
        public void CancellationByTheCallerIsNotRetried()
        {
            using var cancellation = new CancellationTokenSource();
            // The caller cancels while the request is running
            var handler = new QueuedHandler(HttpStatusCode.OK) { OnCall = cancellation.Cancel };

            Assert.That(
                async () => await Send(handler, cancellation.Token).ConfigureAwait(false),
                Throws.InstanceOf<OperationCanceledException>()
            );
            Assert.That(handler.Calls, Is.EqualTo(1));
        }

        private static async Task<HttpResponseMessage> Send(
            HttpMessageHandler handler,
            CancellationToken cancellationToken = default
        )
        {
            using var client = new HttpClient(
                new HttpRetryMessageHandler(handler, (_) => TimeSpan.Zero)
            );
            return await client
                .GetAsync("https://replay.pokemonshowdown.com/test.json", cancellationToken)
                .ConfigureAwait(false);
        }

        private sealed class QueuedHandler(params HttpStatusCode[] statusCodes) : HttpMessageHandler
        {
            private readonly Queue<HttpStatusCode> _statusCodes = new(statusCodes);

            public int Calls { get; private set; }

            /// <summary>
            /// Number of first calls failing with a network error.
            /// </summary>
            public int FailingCalls { get; init; }

            public Action? OnCall { get; init; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                Calls++;
                OnCall?.Invoke();
                cancellationToken.ThrowIfCancellationRequested();
                if (Calls <= FailingCalls)
                {
                    throw new HttpRequestException("Connection refused");
                }
                return Task.FromResult(new HttpResponseMessage(_statusCodes.Dequeue()));
            }
        }
    }
}
