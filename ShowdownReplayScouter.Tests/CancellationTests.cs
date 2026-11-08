#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NUnit.Framework;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class CancellationTests
    {
        /// <summary>
        /// Long enough that no test run finishes all replays before it is canceled.
        /// </summary>
        private const int ReplayCount = 500;

        private HttpClient? _originalHttpClient;

        [SetUp]
        public void SetUp()
        {
            _originalHttpClient = Common.HttpClient;
        }

        [TearDown]
        public void TearDown()
        {
            if (_originalHttpClient is not null)
            {
                Common.HttpClient = _originalHttpClient;
            }
        }

        [Test]
        public async Task ScoutReplaysAsync_UserSearchCanceled_StopsRequestingReplays()
        {
            var handler = new SlowShowdownHandler(LoadReplayJson());
            Common.HttpClient = new HttpClient(handler);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

            var scouting = new Core.ReplayScouter.ShowdownReplayScouter().ScoutReplaysAsync(
                new ScoutingRequest { Users = ["fulllifegames"], MaxConcurrentReplayAnalysis = 2 },
                cancellation.Token
            );

            Assert.That(async () => await scouting, Throws.InstanceOf<OperationCanceledException>());
            await AssertNoMoreRequestsAsync(handler).ConfigureAwait(false);
        }

        [Test]
        public async Task ScoutReplaysAsync_LinksCanceled_StopsRequestingReplays()
        {
            var handler = new SlowShowdownHandler(LoadReplayJson());
            Common.HttpClient = new HttpClient(handler);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            var links = Enumerable
                .Range(0, ReplayCount)
                .Select((index) => new Uri($"https://replay.pokemonshowdown.com/gen7ou-{index}"))
                .ToList();

            var scouting = new Core.ReplayScouter.ShowdownReplayScouter().ScoutReplaysAsync(
                new ScoutingRequest { Links = links, MaxConcurrentReplayAnalysis = 2 },
                cancellation.Token
            );

            Assert.That(async () => await scouting, Throws.InstanceOf<OperationCanceledException>());
            await AssertNoMoreRequestsAsync(handler).ConfigureAwait(false);
        }

        [Test]
        public async Task ScoutReplaysAsync_SearchFails_DropsTheQueuedReplays()
        {
            // The second search page fails, the 51 replays of the first page are already queued
            var handler = new SlowShowdownHandler(LoadReplayJson(), failSecondSearchPage: true);
            Common.HttpClient = new HttpClient(handler);

            var scouting = new Core.ReplayScouter.ShowdownReplayScouter().ScoutReplaysAsync(
                new ScoutingRequest { Users = ["fulllifegames"], MaxConcurrentReplayAnalysis = 2 }
            );

            Assert.That(async () => await scouting, Throws.InstanceOf<HttpRequestException>());
            await AssertNoMoreRequestsAsync(handler).ConfigureAwait(false);
            Assert.That(handler.ReplayRequestCount, Is.LessThan(51));
        }

        private static async Task AssertNoMoreRequestsAsync(SlowShowdownHandler handler)
        {
            // Requests that were already running when it stopped may still finish
            await Task.Delay(TimeSpan.FromMilliseconds(300)).ConfigureAwait(false);
            var countAfterStop = handler.ReplayRequestCount;
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

            Assert.That(handler.ReplayRequestCount, Is.EqualTo(countAfterStop));
            Assert.That(countAfterStop, Is.LessThan(ReplayCount / 2));
        }

        private static string LoadReplayJson()
        {
            return File.ReadAllText(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "Fixtures",
                    "GoldenMaster",
                    "basic-user-search",
                    "replays",
                    "gen7ou-904469859.json"
                )
            );
        }

        /// <summary>
        /// Serves search pages (51 entries, newest first) instantly and every replay after 100 ms.
        /// </summary>
        private sealed class SlowShowdownHandler(string replayJson, bool failSecondSearchPage = false)
            : HttpMessageHandler
        {
            private const long NewestUploadTime = 1_700_000_000;
            private int _replayRequestCount;

            public int ReplayRequestCount => _replayRequestCount;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                var uri = request.RequestUri!;
                if (uri.AbsolutePath == "/search.json")
                {
                    var before = HttpUtility.ParseQueryString(uri.Query)["before"];
                    if (before is not null && failSecondSearchPage)
                    {
                        throw new HttpRequestException("Search page failed");
                    }
                    var start =
                        before is null ? 0 : (int)((NewestUploadTime - long.Parse(before)) / 60) + 1;
                    var entries = Enumerable
                        .Range(start, Math.Max(0, Math.Min(51, ReplayCount - start)))
                        .Select(
                            (index) =>
                                $"{{\"uploadtime\":{NewestUploadTime - (index * 60L)},\"id\":\"gen7ou-{index}\","
                                + $"\"format\":\"gen7ou\",\"players\":[\"FullLifeGames\",\"opponent{index}\"]}}"
                        );
                    return Json($"[{string.Join(',', entries)}]");
                }

                Interlocked.Increment(ref _replayRequestCount);
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken)
                    .ConfigureAwait(false);
                return Json(replayJson);
            }

            private static HttpResponseMessage Json(string content)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}
