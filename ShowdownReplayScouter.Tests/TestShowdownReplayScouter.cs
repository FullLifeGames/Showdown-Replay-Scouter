using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Tests
{
    [TestFixture]
    public class TestShowdownReplayScouter
    {
        private Core.ReplayScouter.ReplayScouter _replayScouter;

        [SetUp]
        public void SetUp()
        {
            _replayScouter = new Core.ReplayScouter.ShowdownReplayScouter();
        }

        [Test]
        public void Scout_FullLifeGames_Gen7Ou_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Tiers = new List<string> { "gen7ou" }
            });

            Assert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_Multiple_Gen7Ou_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames", "Senor L" },
                Tiers = new List<string> { "gen7ou" }
            });

            Assert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_Gen7Ou_Broken_Link_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Links = new List<Uri> { new Uri("https://replay.pokemonshowdown.com/gen7ou-826668378") }
            });
            Assert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_FullLifeGames_MultipleTiers_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Tiers = new List<string> { "gen7ou", "gen8ou" }
            });

            Assert.IsTrue(
                result.Teams.Any((team) => team.Replays.Any((replay) => replay.Link.ToString().Contains("gen7ou")))
                &&
                result.Teams.Any((team) => team.Replays.Any((replay) => replay.Link.ToString().Contains("gen8ou")))
            );
        }

        [Test]
        public void Scout_FullLifeGames_Single_Gen7Ou_Form_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Links = new List<Uri>()
                {
                    new Uri("https://replay.pokemonshowdown.com/future-gen7ou-2227")
                }
            });

            Assert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_FullLifeGames_Single_Gen7Ou_Nickname_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Links = new List<Uri>()
                {
                    new Uri("http://replay.pokemonshowdown.com/smogtours-gen7ou-450650")
                }
            });

            Assert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_FullLifeGames_Single_Gen7Ubers_Form_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Links = new List<Uri>()
                {
                    new Uri("https://replay.pokemonshowdown.com/gen7pokebankubers-499462699")
                }
            });

            Assert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_No_User_Set_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri>()
                {
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen8ou-551237"),
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen8ou-551243"),
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen8ou-551299"),
                }
            });

            Assert.IsTrue(result.Teams.Count() == 4 && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_Broken_User_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "relous" },
                Tiers = new List<string>()
            });

            Assert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_Broken_Link_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri>()
                {
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen8ou-551237"),
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen8ou-66666666"),
                }
            });

            Assert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_Static_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "Trashuny3500" },
                Links = new List<Uri>()
                {
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen8nationaldex-599393"),
                }
            });

            Assert.IsTrue(result.Teams.Any()
                && result.Teams.First().Pokemon.Count == 6
                && result.Teams.First().Pokemon.First((pokemon) => pokemon.Name == "Ferrothorn").Ability != "Static"
            );

            result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "Mollymiltoast" },
                Links = new List<Uri>()
                {
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen8nationaldex-599393"),
                }
            });

            Assert.IsTrue(result.Teams.Any()
                && result.Teams.First().Pokemon.Count == 6
                && result.Teams.First().Pokemon.First((pokemon) => pokemon.Name == "Zapdos").Ability == "Static"
            );
        }

        [Test]
        public void Scout_No_Struggle_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "false" },
                Tiers = new List<string> { "gen1ou" }
            });

            Assert.IsTrue(
                result.Teams.Any() && !result.Teams.Any((team) =>
                    team.Pokemon.Any((pokemon) =>
                        pokemon.Moves.Any((move) =>
                            move == "Struggle"
                        )
                    )
                )
            );
        }

        [Test]
        public void Scout_Correct_ZMoves_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "dflo" },
                Tiers = new List<string> { "gen7ou" }
            });

            Assert.IsTrue(
                result.Teams.Any() &&
                !result.Teams.Any((team) =>
                    team.Pokemon.Any((pokemon) =>
                        pokemon.Moves.Any((move) =>
                            move == "Bloom Doom"
                        )
                    )
                )
                && result.Teams.Any((team) =>
                    team.Pokemon.Any((pokemon) =>
                        pokemon.Item == "Grassium Z"
                    )
                )
            );
        }

        [Test]
        public void Scout_Terastallize()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "hyane" },
                Links = new List<Uri> { new Uri("https://replay.pokemonshowdown.com/gen9ou-1715256459-wskzpoa3vb4nkhvw6p98krvberlafjppw") }
            });

            Assert.IsTrue(
                result.Teams.Any() &&
                result.Teams.Any((team) =>
                    team.Pokemon.Any((pokemon) =>
                        pokemon.TeraType == "Fighting"
                    )
                )
            );
        }

        [Test]
        public void Scout_Only_Tier()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Tiers = new List<string> { "gen7ou" }
            });

            Assert.IsTrue(
                result.Teams.Any()
            );
        }

        [Test]
        public void Scout_Ties()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> { new Uri("https://replay.pokemonshowdown.com/smogtours-gen2ou-672504") }
            });

            Assert.IsTrue(
                result.Teams.Any()
            );
            Assert.IsTrue(
                result.Teams.First().Replays.Any()
            );
            Assert.IsTrue(
                result.Teams.First().Replays.First().Winner == null
            );
        }

        [Test]
        public void Scout_Different_Teams_If_Different_Leads()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> { 
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen2ou-674735"),
                    new Uri("https://replay.pokemonshowdown.com/smogtours-gen2ou-674874")
                }
            });

            Assert.IsTrue(
                result.Teams.Count() == 4
            );
        }

        [Test]
        public void Scout_Identify_Correct_Form()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> {
                    new Uri("https://replay.pokemonshowdown.com/gen9pu-1817019814-mc4x59eeak3fukn0x8qwub7rg1na09upw")
                }
            });

            Assert.IsTrue(
                result.Teams.Count() == 2
                && !result.Teams.Any((team) => team.Pokemon.Any((pokemon) => pokemon.Name == "Zorua"))
                && result.Teams.Any((team) => team.Pokemon.Any((pokemon) => pokemon.Name == "Zorua-Hisui"))
            );
        }
    }
}
