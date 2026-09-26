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
using Newtonsoft.Json;
using NUnit.Framework;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.TeamMergers;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Tests
{
    /// <summary>
    /// Regression tests for the GitHub issues, based on the replays linked in them.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class IssueReplayTests
    {
        private HttpClient? _originalHttpClient;

        [SetUp]
        public void SetUp()
        {
            _originalHttpClient = Common.HttpClient;
            Common.HttpClient = new HttpClient(new IssueFixtureHandler());
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
        public async Task Issue21_DeltaStreamIsAttributedToItsHolder()
        {
            var (_, playerTwo) = await AnalyzeAsync("gen9metronomebattle-2092010901")
                .ConfigureAwait(false);

            Assert.That(PokemonByName(playerTwo, "Glastrier").Ability, Is.EqualTo("Delta Stream"));
        }

        [Test]
        public async Task Issue20_ToxicChainIsAttributedToTheInflictingPokemon()
        {
            var (playerOne, playerTwo) = await AnalyzeAsync("gen9metronomebattle-2094208048")
                .ConfigureAwait(false);

            Assert.That(PokemonByName(playerOne, "Glastrier").Ability, Is.EqualTo("Delta Stream"));
            Assert.That(PokemonByName(playerTwo, "Ting-Lu").Ability, Is.EqualTo("Toxic Chain"));
        }

        [Test]
        public async Task Issue19_FlowerVeilIsAttributedToItsHolder()
        {
            var (_, playerTwo) = await AnalyzeAsync("gen9metronomebattle-2073194180")
                .ConfigureAwait(false);

            Assert.That(
                PokemonByNickname(playerTwo, "Tirnanog").Ability,
                Is.EqualTo("Dauntless Shield")
            );
            Assert.That(
                PokemonByNickname(playerTwo, "Foresight").Ability,
                Is.EqualTo("Flower Veil")
            );
        }

        [Test]
        public async Task Issue18_PoltergeistRevealsTheItemOfTheTarget()
        {
            var (playerOne, _) = await AnalyzeAsync("gen9metronomebattle-2096008516")
                .ConfigureAwait(false);

            Assert.That(PokemonByNickname(playerOne, "Ampharos").Item, Is.EqualTo("Covert Cloak"));
        }

        [Test]
        public async Task Issue17_ZMetronomeRevealsNormaliumZAndIgnoresCalledMoves()
        {
            var (_, playerTwo) = await AnalyzeAsync(
                    "gen9metronomebattle-2075961355-c5pl90go6rxfxtglvcqpoqg3mvf7wgbpw"
                )
                .ConfigureAwait(false);

            var diancie = PokemonByNickname(playerTwo, "Diancie");
            Assert.That(diancie.Item, Is.EqualTo("Normalium Z"));
            Assert.That(diancie.Moves, Is.EqualTo(new[] { "Metronome" }));
        }

        [Test]
        public async Task Issue16_SameSpeciesTwiceIsKeptAsTwoPokemon()
        {
            var (playerOne, _) = await AnalyzeAsync("gen9metronomebattle-2080174920")
                .ConfigureAwait(false);

            Assert.That(playerOne.Pokemon, Has.Count.EqualTo(2));
            Assert.That(
                playerOne.Pokemon.Select((pokemon) => pokemon.Name),
                Is.All.EqualTo("Heracross-Mega")
            );
            Assert.That(
                PokemonByNickname(playerOne, "degen"),
                Is.Not.SameAs(PokemonByNickname(playerOne, "hyper offense"))
            );

            var merged = new ShowdownTeamMerger().MergeTeams([playerOne, playerOne]).Single();
            Assert.That(merged.Pokemon, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task Issue14_PostedTeamsAreUsedAsTheActualSets()
        {
            var (playerOne, playerTwo) = await AnalyzeAsync("gen9draft-2058494320")
                .ConfigureAwait(false);

            var slowking = PokemonByNickname(playerTwo, "Slow Shadow");
            Assert.That(slowking.Item, Is.EqualTo("Colbur Berry"));
            Assert.That(slowking.Ability, Is.EqualTo("Regenerator"));
            Assert.That(slowking.TeraType, Is.EqualTo("Water"));
            Assert.That(
                slowking.Moves,
                Is.EqualTo(new[] { "Chilly Reception", "Scald", "Grass Knot", "Psyshock" })
            );

            var kyurem = PokemonByNickname(playerOne, "PBA S 2 3 & 5");
            Assert.That(kyurem.Item, Is.EqualTo("Heavy-Duty Boots"));
            Assert.That(
                kyurem.Moves,
                Is.EqualTo(new[] { "Ice Beam", "Freeze-Dry", "Earth Power", "Iron Head" })
            );

            Assert.That(playerOne.Pokemon, Has.Count.EqualTo(6));
            Assert.That(playerTwo.Pokemon, Has.Count.EqualTo(6));
            Assert.That(
                playerOne.Pokemon.Concat(playerTwo.Pokemon),
                Has.All.Matches<Pokemon>(
                    (pokemon) =>
                        pokemon.Item != null
                        && pokemon.Ability != null
                        && pokemon.TeraType != null
                        && pokemon.Moves.Count == 4
                )
            );
        }

        [Test]
        public async Task Issue9_TrickedItemsAreAttributedToTheirOriginalHolder()
        {
            var (playerOne, playerTwo) = await AnalyzeAsync("gen9customgame-reconstructed-trick")
                .ConfigureAwait(false);

            Assert.That(PokemonByName(playerTwo, "Rotom-Wash").Item, Is.EqualTo("Choice Scarf"));
            Assert.That(PokemonByName(playerOne, "Vileplume").Item, Is.EqualTo("Black Sludge"));
        }

        [Test]
        public async Task FriskRevealsTheItemOfTheOpponent()
        {
            var (playerOne, playerTwo) = await AnalyzeAsync("gen9customgame-reconstructed-trick")
                .ConfigureAwait(false);

            Assert.That(PokemonByName(playerOne, "Noivern").Ability, Is.EqualTo("Frisk"));
            Assert.That(PokemonByName(playerTwo, "Cobalion").Item, Is.EqualTo("Salac Berry"));
            Assert.That(PokemonByName(playerTwo, "Iron Valiant").Item, Is.EqualTo("Life Orb"));
        }

        [Test]
        public async Task MovesCalledBySleepTalkAreTracked()
        {
            var (_, playerTwo) = await AnalyzeAsync("gen9customgame-reconstructed-trick")
                .ConfigureAwait(false);

            Assert.That(
                PokemonByName(playerTwo, "Rhydon").Moves,
                Is.EquivalentTo(new[] { "Avalanche", "Rest", "Sleep Talk", "Earthquake" })
            );
        }

        [Test]
        public async Task MovesPreventedByCantAreTracked()
        {
            var (playerOne, playerTwo) = await AnalyzeAsync("sim-gen9-cant-showset")
                .ConfigureAwait(false);

            // "|cant|p2a: Clefable|move: Taunt|Calm Mind"
            Assert.That(PokemonByName(playerTwo, "Clefable").Moves, Does.Contain("Calm Mind"));
            // "|cant|p2a: Tsareena|ability: Queenly Majesty|Extreme Speed|[of] p1a: Bob's Bane"
            Assert.That(
                PokemonByName(playerTwo, "Tsareena").Moves,
                Does.Not.Contain("Extreme Speed")
            );
            Assert.That(
                PokemonByName(playerTwo, "Tsareena").Ability,
                Is.EqualTo("Queenly Majesty")
            );
            Assert.That(PokemonByName(playerOne, "Dragonite").Moves, Does.Contain("Extreme Speed"));
        }

        [Test]
        public async Task PostedSingleSetsAreUsedAsTheActualSet()
        {
            var (playerOne, _) = await AnalyzeAsync("sim-gen9-cant-showset").ConfigureAwait(false);

            var dragonite = PokemonByNickname(playerOne, "Bob's Bane");
            Assert.That(dragonite.Item, Is.EqualTo("Leftovers"));
            Assert.That(dragonite.Ability, Is.EqualTo("Multiscale"));
            Assert.That(
                dragonite.Moves,
                Is.EqualTo(new[] { "Extreme Speed", "Dragon Dance", "Earthquake", "Roost" })
            );
            Assert.That(PokemonByName(playerOne, "Grimmsnarl").Item, Is.Null);
        }

        [Test]
        public async Task MovesOfTransformedPokemonAreNotTracked()
        {
            var (playerOne, _) = await AnalyzeAsync("sim-gen9-transform").ConfigureAwait(false);

            var ditto = PokemonByName(playerOne, "Ditto");
            Assert.That(ditto.Ability, Is.EqualTo("Imposter"));
            Assert.That(ditto.Moves, Is.Empty);
            Assert.That(PokemonByName(playerOne, "Mew").Moves, Is.EqualTo(new[] { "Transform" }));
        }

        [Test]
        public async Task MovesUsedWhileDisguisedByIllusionBelongToZoroark()
        {
            var (_, playerTwo) = await AnalyzeAsync("sim-gen9-illusion-forewarn")
                .ConfigureAwait(false);

            Assert.That(playerTwo.Pokemon, Has.Count.EqualTo(2));
            Assert.That(
                PokemonByName(playerTwo, "Zoroark").Moves,
                Is.EqualTo(new[] { "Nasty Plot" })
            );
            Assert.That(PokemonByName(playerTwo, "Hypno").Moves, Is.EqualTo(new[] { "Toxic" }));
            Assert.That(PokemonByName(playerTwo, "Hypno").Ability, Is.EqualTo("Forewarn"));
        }

        [Test]
        public async Task ForewarnRevealsAMoveOfTheOpponent()
        {
            var (playerOne, _) = await AnalyzeAsync("sim-gen9-illusion-forewarn")
                .ConfigureAwait(false);

            Assert.That(
                PokemonByName(playerOne, "Blissey").Moves,
                Is.EquivalentTo(new[] { "Seismic Toss", "Soft-Boiled" })
            );
        }

        [Test]
        public async Task MovesCalledByOtherMovesAreNotTracked()
        {
            var (playerOne, _) = await AnalyzeAsync("sim-gen7-called-moves-zstatus")
                .ConfigureAwait(false);

            Assert.That(
                PokemonByName(playerOne, "Liepard").Moves,
                Is.EqualTo(new[] { "Assist", "Copycat", "Magic Coat" })
            );
        }

        [Test]
        public async Task ZPoweredStatusMovesRevealTheZCrystalOfTheirType()
        {
            var (playerOne, _) = await AnalyzeAsync("sim-gen7-called-moves-zstatus")
                .ConfigureAwait(false);

            var breloom = PokemonByName(playerOne, "Breloom");
            Assert.That(breloom.Item, Is.EqualTo("Grassium Z"));
            Assert.That(breloom.Moves, Is.EqualTo(new[] { "Spore" }));
        }

        [Test]
        public async Task MaxMovesAreNotTracked()
        {
            var (playerOne, _) = await AnalyzeAsync("sim-gen8-dynamax").ConfigureAwait(false);

            Assert.That(
                PokemonByName(playerOne, "Charizard").Moves,
                Is.EqualTo(new[] { "Air Slash" })
            );
        }

        [Test]
        public async Task TeamsCachedByAnOlderAnalysisAreNotReused()
        {
            var replayId = "gen9metronomebattle-2092010901";
            var jsonLink = $"https://replay.pokemonshowdown.com/{replayId}.json";
            var cache = new DictionaryDistributedCache();
            var staleTeam = JsonConvert.SerializeObject(
                new Team { Pokemon = [new Pokemon { Name = "Glastrier", Ability = "Toxic Chain" }] }
            );
            await cache.SetStringAsync($"{jsonLink}+p1", staleTeam).ConfigureAwait(false);
            await cache.SetStringAsync($"{jsonLink}+p2", staleTeam).ConfigureAwait(false);

            var teams = (
                await new ShowdownReplayAnalyzer(cache)
                    .AnalyzeReplayAsync(new Uri($"https://replay.pokemonshowdown.com/{replayId}"))
                    .ConfigureAwait(false)
            ).ToList();

            Assert.That(PokemonByName(teams[1], "Glastrier").Ability, Is.EqualTo("Delta Stream"));
            Assert.That(
                await cache
                    .GetStringAsync($"{jsonLink}+v{ShowdownReplayAnalyzer.AnalysisVersion}+p2")
                    .ConfigureAwait(false),
                Does.Contain("Delta Stream")
            );
        }

        [Test]
        public async Task UnknownFormsAreMatchedToTheirBasePokemon()
        {
            // Pokemon referenced before they switched in, e.g. because of a broken switch line
            var (playerOne, _) = await AnalyzeAsync("synthetic-form-fallback")
                .ConfigureAwait(false);

            Assert.That(PokemonByName(playerOne, "Zoroark-Hisui").Ability, Is.EqualTo("Illusion"));
            Assert.That(
                PokemonByName(playerOne, "Tauros-Paldea-Combat").Ability,
                Is.EqualTo("Intimidate")
            );
            Assert.That(PokemonByName(playerOne, "Ho-Oh").Ability, Is.EqualTo("Pressure"));

            // Porygon-Z is its own species and no form of Porygon
            Assert.That(PokemonByName(playerOne, "Porygon").Ability, Is.Null);
            Assert.That(PokemonByName(playerOne, "Porygon").FormName, Is.Null);
            Assert.That(PokemonByName(playerOne, "Porygon-Z").Ability, Is.EqualTo("Adaptability"));
            Assert.That(playerOne.Pokemon, Has.Count.EqualTo(5));
        }

        [Test]
        public void Issue13_MergingTeamsDoesNotDuplicateTeraTypes()
        {
            var teams = new[] { "Flying", "Flying", "Water" }.Select(
                (teraType, index) =>
                    new Team
                    {
                        Format = "gen9ou",
                        Pokemon =
                        [
                            new Pokemon
                            {
                                Name = "Glastrier",
                                TeraType = teraType,
                                Item = "Leftovers",
                                Ability = "Chilling Neigh"
                            }
                        ],
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
                                Players = ["player-one", "player-two"]
                            }
                        ]
                    }
            );

            var glastrier = new ShowdownTeamMerger().MergeTeams(teams).Single().Pokemon.Single();

            Assert.That(glastrier.TeraType, Is.EqualTo("Flying | Water"));
            Assert.That(glastrier.Item, Is.EqualTo("Leftovers"));
            Assert.That(glastrier.Ability, Is.EqualTo("Chilling Neigh"));
        }

        private static async Task<(Team PlayerOne, Team PlayerTwo)> AnalyzeAsync(string replayId)
        {
            var teams = (
                await new ShowdownReplayAnalyzer()
                    .AnalyzeReplayAsync(new Uri($"https://replay.pokemonshowdown.com/{replayId}"))
                    .ConfigureAwait(false)
            ).ToList();
            return (teams[0], teams[1]);
        }

        private static Pokemon PokemonByName(Team team, string name)
        {
            return team.Pokemon.Single(
                (pokemon) => pokemon.Name == name || pokemon.FormName == name
            );
        }

        private static Pokemon PokemonByNickname(Team team, string nickname)
        {
            return team.Pokemon.Single((pokemon) => pokemon.AltNames.Contains(nickname));
        }

        private sealed class IssueFixtureHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                var fixturePath = Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "Fixtures",
                    "Issues",
                    request.RequestUri!.AbsolutePath.TrimStart('/')
                );
                if (!File.Exists(fixturePath))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            File.ReadAllText(fixturePath),
                            Encoding.UTF8,
                            "application/json"
                        )
                    }
                );
            }
        }

        private sealed class DictionaryDistributedCache : IDistributedCache
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
    }
}
