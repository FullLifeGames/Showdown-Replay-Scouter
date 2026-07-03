#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using NUnit.Framework;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class PerformanceOptimizationTests
    {
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
        public async Task AnalyzeReplayAsyncWithoutUser_FetchesReplayJsonOnceForBothPlayers()
        {
            var handler = new CountingHttpMessageHandler(_ => ReplayJson());
            Common.HttpClient = new HttpClient(handler);

            var analyzer = new ShowdownReplayAnalyzer();
            var teams = (await analyzer
                .AnalyzeReplayAsync(new Uri("https://replay.pokemonshowdown.com/gen9ou-1"))
                .ConfigureAwait(false)).ToList();

            Assert.That(teams, Has.Count.EqualTo(2));
            Assert.That(teams.All(team => team.Pokemon.Count == 1), Is.True);
            Assert.That(handler.RequestCount, Is.EqualTo(1));
        }

        [Test]
        public async Task CollectReplaysAsync_WithMinimumDate_StopsAfterPageCrossesMinimumDate()
        {
            var handler = new CountingHttpMessageHandler(request =>
            {
                var query = request.RequestUri?.Query ?? "";
                if (query.Contains("before=150"))
                {
                    return SearchPage(firstUploadTime: 90, count: 51);
                }
                if (query.Contains("before=40"))
                {
                    return SearchPage(firstUploadTime: 39, count: 0);
                }
                return SearchPage(firstUploadTime: 200, count: 51);
            });
            Common.HttpClient = new HttpClient(handler);

            var collector = new ApiShowdownReplayCollector();
            var request = new ScoutingRequest
            {
                Users = ["alice"],
                MinimumDate = DateTimeOffset.FromUnixTimeSeconds(100).UtcDateTime
            };

            var replays = new List<CollectedReplay>();
            await foreach (var replay in collector.CollectReplaysAsync(request))
            {
                replays.Add(replay);
            }

            Assert.That(replays, Has.Count.EqualTo(51));
            Assert.That(
                handler.RequestedUris.Any(uri => uri.Query.Contains("before=40")),
                Is.False
            );
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        }

        [Test]
        public async Task CollectReplaysAsync_WithMaximumDate_StartsSearchBeforeMaximumDate()
        {
            var handler = new CountingHttpMessageHandler(_ => SearchPage(firstUploadTime: 0, count: 0));
            Common.HttpClient = new HttpClient(handler);

            var collector = new ApiShowdownReplayCollector();
            var maximumDate = DateTimeOffset.FromUnixTimeSeconds(123).UtcDateTime;

            await foreach (
                var _ in collector.CollectReplaysAsync(
                    new ScoutingRequest { Users = ["alice"], MaximumDate = maximumDate }
                )
            ) { }

            Assert.That(handler.RequestedUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestedUris[0].Query, Does.Contain("before=124"));
        }

        [Test]
        public async Task ScoutReplaysAsync_HonorsRequestedReplayAnalysisConcurrency()
        {
            var requestedConcurrency = Environment.ProcessorCount + 2;
            var analyzer = new ConcurrencyTrackingReplayAnalyzer(requestedConcurrency);
            var scouter = new TestReplayScouter(
                analyzer,
                new FixedReplayCollector(requestedConcurrency),
                new ShowdownTeamMerger()
            );

            var scoutingTask = scouter.ScoutReplaysAsync(
                new ScoutingRequest
                {
                    Users = ["alice"],
                    Grouped = false,
                    MaxConcurrentReplayAnalysis = requestedConcurrency
                }
            );

            await analyzer.WaitForExpectedConcurrencyAsync().ConfigureAwait(false);
            analyzer.Release();
            await scoutingTask.ConfigureAwait(false);

            Assert.That(analyzer.MaxObservedConcurrency, Is.EqualTo(requestedConcurrency));
        }

        [Test]
        public void ShowdownReplayScouter_ReusesCoreServiceInstances()
        {
            var scouter = new Core.ReplayScouter.ShowdownReplayScouter();

            Assert.That(
                ReferenceEquals(scouter.ReplayAnalyzer, scouter.ReplayAnalyzer),
                Is.True
            );
            Assert.That(
                ReferenceEquals(scouter.ReplayCollector, scouter.ReplayCollector),
                Is.True
            );
            Assert.That(ReferenceEquals(scouter.TeamMerger, scouter.TeamMerger), Is.True);
        }

        [Test]
        public void MergeTeams_ComputesEachTeamDefinitionOnce()
        {
            CountingTeam.ToStringCalls = 0;
            var teams = Enumerable.Range(0, 30).Select(CountingTeam.Create).ToList();

            _ = new ShowdownTeamMerger().MergeTeams(teams).ToList();

            Assert.That(CountingTeam.ToStringCalls, Is.LessThanOrEqualTo(teams.Count * 2));
        }

        [Test]
        public async Task CacheCollector_BatchesStringWritesUntilStore()
        {
            var innerCache = new CountingDistributedCache();
            var cacheCollector = new CacheCollector(innerCache);
            var distributedCache = (IDistributedCache)cacheCollector;

            await distributedCache
                .SetStringAsync("replay-key", "cached-team")
                .ConfigureAwait(false);

            Assert.That(innerCache.SetCount, Is.EqualTo(0));
            Assert.That(
                await distributedCache.GetStringAsync("replay-key").ConfigureAwait(false),
                Is.EqualTo("cached-team")
            );

            cacheCollector.Store();

            Assert.That(innerCache.SetCount, Is.EqualTo(1));
            Assert.That(innerCache.GetString("replay-key"), Is.EqualTo("cached-team"));
        }

        private static string ReplayJson()
        {
            return """
                {
                  "id": "gen9ou-1",
                  "p1": "Alice",
                  "p2": "Bob",
                  "format": "gen9ou",
                  "log": "|player|p1|Alice|\n|player|p2|Bob|\n|poke|p1|Pikachu, M|\n|poke|p2|Charizard, M|\n|win|Alice",
                  "uploadTime": 100,
                  "views": 1,
                  "formatId": "gen9ou",
                  "private": 0,
                  "players": ["Alice", "Bob"]
                }
                """;
        }

        private static string SearchPage(long firstUploadTime, int count)
        {
            var entries = Enumerable
                .Range(0, count)
                .Select(index =>
                {
                    var uploadTime = firstUploadTime - index;
                    return new
                    {
                        uploadtime = uploadTime,
                        id = $"gen9ou-{uploadTime}",
                        format = "gen9ou",
                        players = new[] { "alice", "bob" }
                    };
                });

            return JsonSerializer.Serialize(entries);
        }

        private sealed class CountingHttpMessageHandler(
            Func<HttpRequestMessage, string> responseFactory
        ) : HttpMessageHandler
        {
            private readonly List<Uri> _requestedUris = [];

            public IReadOnlyList<Uri> RequestedUris => _requestedUris;

            public int RequestCount => _requestedUris.Count;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                if (request.RequestUri is not null)
                {
                    _requestedUris.Add(request.RequestUri);
                }

                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            responseFactory(request),
                            Encoding.UTF8,
                            "application/json"
                        )
                    }
                );
            }
        }

        private sealed class CountingTeam : Team
        {
            public static int ToStringCalls { get; set; }

            public static CountingTeam Create(int index)
            {
                return new CountingTeam
                {
                    Pokemon = [new Pokemon { Name = $"Pokemon-{index}" }],
                    Replays =
                    [
                        new Replay
                        {
                            Id = $"gen9ou-{index}",
                            Format = "gen9ou",
                            FormatId = "gen9ou",
                            Link = new Uri(
                                $"https://replay.pokemonshowdown.com/gen9ou-{index}"
                            ),
                            Log = "",
                            Players = ["alice", "bob"]
                        }
                    ]
                };
            }

            public override string ToString()
            {
                ToStringCalls++;
                return base.ToString();
            }
        }

        private sealed class TestReplayScouter(
            IReplayAnalyzer analyzer,
            IReplayCollector collector,
            ITeamMerger teamMerger
        ) : Core.ReplayScouter.ReplayScouter
        {
            public override IReplayAnalyzer ReplayAnalyzer { get; } = analyzer;
            public override IReplayCollector ReplayCollector { get; } = collector;
            public override ITeamMerger TeamMerger { get; } = teamMerger;
        }

        private sealed class FixedReplayCollector(int replayCount) : IReplayCollector
        {
            public async IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(
                ScoutingRequest scoutingRequest
            )
            {
                for (var index = 0; index < replayCount; index++)
                {
                    yield return new CollectedReplay(
                        new Uri($"https://replay.pokemonshowdown.com/gen9ou-{index}"),
                        scoutingRequest.Users?.FirstOrDefault()
                    );
                    await Task.Yield();
                }
            }
        }

        private sealed class ConcurrencyTrackingReplayAnalyzer(
            int expectedConcurrency
        ) : IReplayAnalyzer
        {
            private readonly TaskCompletionSource _expectedConcurrencyReached =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _release =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _currentConcurrency;

            public int MaxObservedConcurrency { get; private set; }

            public Task WaitForExpectedConcurrencyAsync()
            {
                return _expectedConcurrencyReached.Task.WaitAsync(TimeSpan.FromSeconds(2));
            }

            public void Release()
            {
                _release.TrySetResult();
            }

            public IEnumerable<Team> AnalyzeReplay(string replay)
            {
                throw new NotSupportedException();
            }

            public Task<IEnumerable<Team>> AnalyzeReplayAsync(string replay)
            {
                throw new NotSupportedException();
            }

            public IEnumerable<Team> AnalyzeReplay(Uri replay)
            {
                throw new NotSupportedException();
            }

            public Task<IEnumerable<Team>> AnalyzeReplayAsync(Uri replay)
            {
                throw new NotSupportedException();
            }

            public IEnumerable<Team> AnalyzeReplay(string replay, string? user)
            {
                throw new NotSupportedException();
            }

            public Task<IEnumerable<Team>> AnalyzeReplayAsync(string replay, string? user)
            {
                throw new NotSupportedException();
            }

            public IEnumerable<Team> AnalyzeReplay(Uri replay, string? user)
            {
                throw new NotSupportedException();
            }

            public async Task<IEnumerable<Team>> AnalyzeReplayAsync(Uri replay, string? user)
            {
                var currentConcurrency = Interlocked.Increment(ref _currentConcurrency);
                if (currentConcurrency > MaxObservedConcurrency)
                {
                    MaxObservedConcurrency = currentConcurrency;
                }
                if (currentConcurrency == expectedConcurrency)
                {
                    _expectedConcurrencyReached.TrySetResult();
                }

                await _release.Task.ConfigureAwait(false);
                Interlocked.Decrement(ref _currentConcurrency);

                return
                [
                    new Team
                    {
                        Pokemon = [new Pokemon { Name = replay.AbsolutePath }],
                        Replays =
                        [
                            new Replay
                            {
                                Id = replay.AbsolutePath,
                                Format = "gen9ou",
                                FormatId = "gen9ou",
                                Link = replay,
                                Log = "",
                                Players = ["alice", "bob"]
                            }
                        ]
                    }
                ];
            }
        }

        private sealed class CountingDistributedCache : IDistributedCache
        {
            private readonly ConcurrentDictionary<string, byte[]> _entries = new();

            public int SetCount { get; private set; }

            public byte[]? Get(string key)
            {
                return _entries.TryGetValue(key, out var value) ? value : null;
            }

            public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            {
                return Task.FromResult(Get(key));
            }

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                SetCount++;
                _entries[key] = value;
            }

            public Task SetAsync(
                string key,
                byte[] value,
                DistributedCacheEntryOptions options,
                CancellationToken token = default
            )
            {
                Set(key, value, options);
                return Task.CompletedTask;
            }

            public void Refresh(string key) { }

            public Task RefreshAsync(string key, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public void Remove(string key)
            {
                _entries.TryRemove(key, out _);
            }

            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                Remove(key);
                return Task.CompletedTask;
            }
        }
    }
}
