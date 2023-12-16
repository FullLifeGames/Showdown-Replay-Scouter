using NUnit.Framework;
using NUnit.Framework.Legacy;
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

            ClassicAssert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_FullLifeGames_MinDate_Gen7Ou_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Tiers = new List<string> { "gen7ou" },
                MaximumDate = new DateTime(2023, 5, 5),
                MinimumDate = new DateTime(2019, 7, 5)
            });

            ClassicAssert.IsTrue(result.Teams.Any());
            ClassicAssert.IsTrue(result.Teams.Count() == 1);
        }

        [Test]
        public void Scout_Urshifu_Form_Issue_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "Unviabriel" },
                Links = new List<Uri> { new("https://replay.pokemonshowdown.com/gen9stabmons-1890825135") }
            });

            ClassicAssert.IsTrue(result.Teams.Any());
            ClassicAssert.IsTrue(result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_New_Format_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> { new("https://replay.pokemonshowdown.com/smogtours-gen9ou-733546") }
            });

            ClassicAssert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_New_Format_Winner_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> { new("https://replay.pokemonshowdown.com/smogtours-gen1ou-733374") }
            });

            ClassicAssert.IsTrue(result.Teams.Any());
            ClassicAssert.IsTrue(
                result.Teams.First().Replays.First().Winner == "Green on fire"
            );
            ClassicAssert.IsTrue(
                result.Teams.Any((team) => team.Replays.First().WinForTeam)
            );
        }

        [Test]
        public void Scout_New_Format_Query_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> { new("https://replay.pokemonshowdown.com/smogtours-gen9ou-735029?p2") }
            });

            ClassicAssert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_Relous_MaxDate_Gen7Ou_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "relous" },
                Tiers = new List<string> { "gen7ou" },
                MaximumDate = new DateTime(2020, 5, 5),
                MinimumDate = new DateTime(2019, 7, 5)
            });

            ClassicAssert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_Multiple_Gen7Ou_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames", "Senor L" },
                Tiers = new List<string> { "gen7ou" }
            });

            ClassicAssert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_Gen7Ou_Broken_Link_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Links = new List<Uri> { new("https://replay.pokemonshowdown.com/gen7ou-826668378") }
            });
            ClassicAssert.IsTrue(result.Teams.Any());
        }

        [Test]
        public void Scout_FullLifeGames_MultipleTiers_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Tiers = new List<string> { "gen7ou", "gen8ou" }
            });

            ClassicAssert.IsTrue(
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
                    new("https://replay.pokemonshowdown.com/future-gen7ou-2227")
                }
            });

            ClassicAssert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_FullLifeGames_Single_Gen7Ou_Nickname_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Links = new List<Uri>()
                {
                    new("http://replay.pokemonshowdown.com/smogtours-gen7ou-450650")
                }
            });

            ClassicAssert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_FullLifeGames_Single_Gen7Ubers_Form_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Links = new List<Uri>()
                {
                    new("https://replay.pokemonshowdown.com/gen7pokebankubers-499462699")
                }
            });

            ClassicAssert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_No_User_Set_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri>()
                {
                    new("https://replay.pokemonshowdown.com/smogtours-gen8ou-551237"),
                    new("https://replay.pokemonshowdown.com/smogtours-gen8ou-551243"),
                    new("https://replay.pokemonshowdown.com/smogtours-gen8ou-551299"),
                }
            });

            ClassicAssert.IsTrue(result.Teams.Count() == 4 && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_Broken_Replay_Gen_9_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri>()
                {
                    new("https://replay.pokemonshowdown.com/smogtours-gen9ou-681522"),
                }
            });

            ClassicAssert.IsTrue(result.Teams.Count() == 2 && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_Broken_User_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "relous" },
                Tiers = new List<string>()
            });

            ClassicAssert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_Broken_Link_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri>()
                {
                    new("https://replay.pokemonshowdown.com/smogtours-gen8ou-551237"),
                    new("https://replay.pokemonshowdown.com/smogtours-gen8ou-66666666"),
                }
            });

            ClassicAssert.IsTrue(result.Teams.Any() && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_Static_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "Trashuny3500" },
                Links = new List<Uri>()
                {
                    new("https://replay.pokemonshowdown.com/smogtours-gen8nationaldex-599393"),
                }
            });

            ClassicAssert.IsTrue(result.Teams.Any()
                && result.Teams.First().Pokemon.Count == 6
                && result.Teams.First().Pokemon.First((pokemon) => pokemon.Name == "Ferrothorn").Ability != "Static"
            );

            result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "Mollymiltoast" },
                Links = new List<Uri>()
                {
                    new("https://replay.pokemonshowdown.com/smogtours-gen8nationaldex-599393"),
                }
            });

            ClassicAssert.IsTrue(result.Teams.Any()
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

            ClassicAssert.IsTrue(
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

            ClassicAssert.IsTrue(
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
                Links = new List<Uri> { new("https://replay.pokemonshowdown.com/gen9ou-1715256459-wskzpoa3vb4nkhvw6p98krvberlafjppw") }
            });

            ClassicAssert.IsTrue(
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

            ClassicAssert.IsTrue(
                result.Teams.Any()
            );
        }

        [Test]
        public void Scout_Ties()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> { new("https://replay.pokemonshowdown.com/smogtours-gen2ou-672504") }
            });

            ClassicAssert.IsTrue(
                result.Teams.Any()
            );
            ClassicAssert.IsTrue(
                result.Teams.First().Replays.Any()
            );
            ClassicAssert.IsTrue(
                result.Teams.First().Replays.First().Winner == null
            );
        }

        [Test]
        public void Scout_Different_Teams_If_Different_Leads()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> {
                    new("https://replay.pokemonshowdown.com/smogtours-gen2ou-674735"),
                    new("https://replay.pokemonshowdown.com/smogtours-gen2ou-674874")
                }
            });

            ClassicAssert.IsTrue(
                result.Teams.Count() == 4
            );
        }

        [Test]
        public void Scout_Identify_Correct_Form()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> {
                    new("https://replay.pokemonshowdown.com/gen9pu-1817019814-mc4x59eeak3fukn0x8qwub7rg1na09upw")
                }
            });

            ClassicAssert.IsTrue(
                result.Teams.Count() == 2
                && !result.Teams.Any((team) => team.Pokemon.Any((pokemon) => pokemon.Name == "Zorua"))
                && result.Teams.Any((team) => team.Pokemon.Any((pokemon) => pokemon.Name == "Zorua-Hisui"))
            );
        }

        [Test]
        public void Scout_Correct_Zorua_Identification()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> {
                    new("https://replay.pokemonshowdown.com/gen9lc-1859090126")
                },
                Users = new List<string> { "Leafium Z" }
            });

            ClassicAssert.IsTrue(
                result.Teams.Count() == 1
                && result.Teams.All((team) => team.Pokemon.Count == 6)
                && result.Teams.Any((team) => team.Pokemon.Any((pokemon) => pokemon.Name == "Zorua-Hisui"))
            );
        }
    }
}
