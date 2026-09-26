#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    }
}
