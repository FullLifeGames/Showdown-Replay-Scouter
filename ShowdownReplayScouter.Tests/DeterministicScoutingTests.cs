#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
    public class DeterministicScoutingTests
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
        public async Task ScoutReplaysAsync_WithFixtureSearch_ReturnsExpectedUserTeams()
        {
            var scenario = GoldenMasterScenario.Load("basic-user-search");
            var handler = new FixtureShowdownHandler(scenario.Responses);
            Common.HttpClient = new HttpClient(handler);

            var result = await new Core.ReplayScouter.ShowdownReplayScouter()
                .ScoutReplaysAsync(
                    new ScoutingRequest
                    {
                        Users = ["fulllifegames"],
                        Tiers = ["gen7ou"],
                        MaximumDate = new DateTime(2023, 5, 5),
                        MinimumDate = new DateTime(2019, 5, 1),
                        Grouped = false,
                        MaxConcurrentReplayAnalysis = 2
                    }
                )
                .ConfigureAwait(false);

            var snapshot = Snapshot(result!.Teams);

            AssertSnapshotMatches(scenario, snapshot);
            Assert.That(handler.RequestCount, Is.EqualTo(4));
        }

        [Test]
        public async Task ScoutReplaysAsync_WithGroupedFixtureLinks_MergesEquivalentTeams()
        {
            var replayOne = new Uri("https://replay.pokemonshowdown.com/gen7ou-856921732");
            var replayTwo = new Uri("https://replay.pokemonshowdown.com/gen7ou-860458474");
            var scenario = GoldenMasterScenario.Load("grouped-links");
            var handler = new FixtureShowdownHandler(scenario.Responses);
            Common.HttpClient = new HttpClient(handler);

            var result = await new Core.ReplayScouter.ShowdownReplayScouter()
                .ScoutReplaysAsync(
                    new ScoutingRequest
                    {
                        Users = ["fulllifegames"],
                        Links = [replayOne, replayTwo],
                        Grouped = true,
                        MaxConcurrentReplayAnalysis = 2
                    }
                )
                .ConfigureAwait(false);

            AssertSnapshotMatches(scenario, Snapshot(result!.Teams));
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        }

        [Test]
        public async Task ScoutReplaysAsync_WithRepresentativeReplayFixtures_ReturnsExpectedTeams()
        {
            var scenario = GoldenMasterScenario.Load("representative-replay-corpus");
            var handler = new FixtureShowdownHandler(scenario.Responses);
            Common.HttpClient = new HttpClient(handler);
            var links = new[]
            {
                new Uri("https://replay.pokemonshowdown.com/smogtours-gen9ou-733546"),
                new Uri("https://replay.pokemonshowdown.com/smogtours-gen1ou-733374"),
                new Uri("https://replay.pokemonshowdown.com/smogtours-gen1ou-734539"),
                new Uri("https://replay.pokemonshowdown.com/smogtours-gen8nationaldex-599393"),
                new Uri("https://replay.pokemonshowdown.com/smogtours-gen2ou-672504"),
                new Uri("https://replay.pokemonshowdown.com/smogtours-gen9ou-681522"),
            };

            var result = await new Core.ReplayScouter.ShowdownReplayScouter()
                .ScoutReplaysAsync(
                    new ScoutingRequest
                    {
                        Links = links,
                        Grouped = false,
                        MaxConcurrentReplayAnalysis = 3
                    }
                )
                .ConfigureAwait(false);

            AssertSnapshotMatches(scenario, Snapshot(result!.Teams));
            Assert.That(handler.RequestCount, Is.EqualTo(links.Length));
        }

        [Test]
        public async Task CacheCollector_PendingWritesOverwriteRemoveAndFlushConsistently()
        {
            var innerCache = new InMemoryDistributedCache();
            var cacheCollector = new CacheCollector(innerCache);
            var distributedCache = (IDistributedCache)cacheCollector;

            await distributedCache.SetStringAsync("team", "first").ConfigureAwait(false);
            await distributedCache.SetStringAsync("team", "second").ConfigureAwait(false);

            Assert.That(innerCache.GetString("team"), Is.Null);
            Assert.That(
                await distributedCache.GetStringAsync("team").ConfigureAwait(false),
                Is.EqualTo("second")
            );

            cacheCollector.Store();

            Assert.That(innerCache.GetString("team"), Is.EqualTo("second"));

            await distributedCache.SetStringAsync("team", "third").ConfigureAwait(false);
            await distributedCache.RemoveAsync("team").ConfigureAwait(false);

            Assert.That(await distributedCache.GetStringAsync("team").ConfigureAwait(false), Is.Null);
            Assert.That(innerCache.GetString("team"), Is.Null);
        }

        [Test]
        public void ShowdownTeamMerger_MergeTeams_PreservesAltNamesFromEveryMergedReplay()
        {
            var teamOne = TeamWithPokemonAltNames(
                "gen7ou-1",
                "Hydreigon",
                ["Hydreigon, M"]
            );
            var teamTwo = TeamWithPokemonAltNames(
                "gen7ou-2",
                "Hydreigon",
                ["Hydreigon", "Hydreigon, F"]
            );

            var mergedTeam = new ShowdownTeamMerger().MergeTeams([teamOne, teamTwo]).Single();

            Assert.That(
                mergedTeam.Pokemon.Single().AltNames,
                Is.EquivalentTo(["Hydreigon, M", "Hydreigon", "Hydreigon, F"])
            );
        }

        [Test]
        public async Task ScoutReplaysAsync_WithHighConcurrency_ReturnsEveryCollectedReplayOnce()
        {
            var replayCount = 250;
            var scouter = new TestReplayScouter(
                new EchoReplayAnalyzer(),
                new FixedReplayCollector(replayCount),
                new ShowdownTeamMerger()
            );

            var result = await scouter
                .ScoutReplaysAsync(
                    new ScoutingRequest
                    {
                        Users = ["alice"],
                        Grouped = false,
                        MaxConcurrentReplayAnalysis = 32
                    }
                )
                .ConfigureAwait(false);

            var replayIds = result!.Teams.Select(team => team.Replays.Single().Id).ToList();

            Assert.That(replayIds, Has.Count.EqualTo(replayCount));
            Assert.That(replayIds.Distinct().Count(), Is.EqualTo(replayCount));
            Assert.That(replayIds, Is.EquivalentTo(Enumerable.Range(0, replayCount).Select(index => $"gen9ou-{index}")));
        }

        private static string Snapshot(IEnumerable<Team> teams)
        {
            return string.Join(
                "\n",
                teams.Select(team =>
                {
                    var replays = team.Replays.OrderBy(replay => replay.Id).ToList();
                    var replayIds = string.Join(",", replays.Select(replay => replay.Id));
                    var playerNames = string.Join(
                        ",",
                        replays.Select(replay => replay.PlayerInfo?.PlayerName)
                    );
                    var winsForTeam = string.Join(
                        ",",
                        replays.Select(replay => replay.WinForTeam)
                    );
                    var pokemonSnapshot = string.Join(
                        ";",
                        team.Pokemon.OrderBy(pokemon => pokemon.Name).Select(pokemon =>
                            string.Join(
                                "|",
                                pokemon.Name,
                                pokemon.Item,
                                pokemon.Ability,
                                pokemon.TeraType,
                                string.Join(",", pokemon.Moves.OrderBy(move => move)),
                                string.Join(",", pokemon.AltNames.OrderBy(altName => altName))
                            )
                        )
                    );

                    return new
                    {
                        ReplayIds = replayIds,
                        team.Format,
                        PlayerNames = playerNames,
                        WinsForTeam = winsForTeam,
                        PokemonSnapshot = pokemonSnapshot
                    };
                })
                .OrderBy(team => team.ReplayIds)
                .ThenBy(team => team.PlayerNames)
                .ThenBy(team => team.PokemonSnapshot)
                .Select(team =>
                {
                    return string.Join(
                        "|",
                        team.ReplayIds,
                        team.Format,
                        team.PlayerNames,
                        team.WinsForTeam,
                        team.PokemonSnapshot
                    );
                })
            );
        }

        private static void AssertSnapshotMatches(GoldenMasterScenario scenario, string snapshot)
        {
            snapshot = GoldenMasterScenario.NormalizeLineEndings(snapshot).Trim();
            if (
                string.Equals(
                    Environment.GetEnvironmentVariable("UPDATE_GOLDEN_MASTER_FIXTURES"),
                    "1",
                    StringComparison.Ordinal
                )
            )
            {
                File.WriteAllText(
                    scenario.SourceExpectedSnapshotPath,
                    snapshot + Environment.NewLine
                );
                Assert.Pass(
                    $"Updated golden-master snapshot at {scenario.SourceExpectedSnapshotPath}."
                );
            }

            Assert.That(snapshot, Is.EqualTo(scenario.ExpectedSnapshot));
        }

        private static Team TeamWithPokemonAltNames(
            string replayId,
            string pokemonName,
            IEnumerable<string> altNames
        )
        {
            return new Team
            {
                Format = "gen7ou",
                Pokemon = [new Pokemon { Name = pokemonName, AltNames = altNames.ToList() }],
                Replays =
                [
                    new Replay
                    {
                        Id = replayId,
                        Format = "gen7ou",
                        FormatId = "gen7ou",
                        Link = new Uri($"https://replay.pokemonshowdown.com/{replayId}"),
                        Log = "",
                        Players = ["player-one", "player-two"]
                    }
                ]
            };
        }

        private sealed class GoldenMasterScenario
        {
            private GoldenMasterScenario(
                IReadOnlyDictionary<string, string> responses,
                string expectedSnapshot,
                string sourceExpectedSnapshotPath
            )
            {
                Responses = responses;
                ExpectedSnapshot = expectedSnapshot;
                SourceExpectedSnapshotPath = sourceExpectedSnapshotPath;
            }

            public IReadOnlyDictionary<string, string> Responses { get; }

            public string ExpectedSnapshot { get; }

            public string SourceExpectedSnapshotPath { get; }

            public static GoldenMasterScenario Load(string scenarioName)
            {
                var scenarioDirectory = Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "Fixtures",
                    "GoldenMaster",
                    scenarioName
                );
                var sourceScenarioDirectory = Path.Combine(
                    Path.GetFullPath(
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..")
                    ),
                    "Fixtures",
                    "GoldenMaster",
                    scenarioName
                );
                var responses = new Dictionary<string, string>();
                var searchPath = Path.Combine(scenarioDirectory, "search.json");
                if (File.Exists(searchPath))
                {
                    responses["search"] = File.ReadAllText(searchPath);
                }

                var replayDirectory = Path.Combine(scenarioDirectory, "replays");
                foreach (var replayPath in Directory.EnumerateFiles(replayDirectory, "*.json"))
                {
                    responses[Path.GetFileName(replayPath)] = File.ReadAllText(replayPath);
                }

                var expectedSnapshotPath = Path.Combine(scenarioDirectory, "expected-snapshot.txt");
                var sourceExpectedSnapshotPath = Path.Combine(
                    Directory.Exists(sourceScenarioDirectory)
                        ? sourceScenarioDirectory
                        : scenarioDirectory,
                    "expected-snapshot.txt"
                );
                var expectedSnapshot = NormalizeLineEndings(File.ReadAllText(expectedSnapshotPath))
                    .Trim();

                return new GoldenMasterScenario(
                    responses,
                    expectedSnapshot,
                    sourceExpectedSnapshotPath
                );
            }

            public static string NormalizeLineEndings(string value)
            {
                return value.Replace("\r\n", "\n").Replace("\r", "\n");
            }
        }

        private sealed class FixtureShowdownHandler(
            IReadOnlyDictionary<string, string> responses
        ) : HttpMessageHandler
        {
            private int _requestCount;

            public int RequestCount => _requestCount;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                Interlocked.Increment(ref _requestCount);
                var requestUri = request.RequestUri!;
                var key = requestUri.AbsolutePath.TrimStart('/');
                if (key == "search.json")
                {
                    key = "search";
                }

                if (!responses.TryGetValue(key, out var response))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(response, Encoding.UTF8, "application/json")
                    }
                );
            }
        }

        private sealed class InMemoryDistributedCache : IDistributedCache
        {
            private readonly ConcurrentDictionary<string, byte[]> _entries = new();

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

        private sealed class EchoReplayAnalyzer : IReplayAnalyzer
        {
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
                await Task.Yield();
                var replayId = replay.AbsolutePath.TrimStart('/');
                return
                [
                    new Team
                    {
                        Pokemon = [new Pokemon { Name = replayId }],
                        Replays =
                        [
                            new Replay
                            {
                                Id = replayId,
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
    }
}
