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
        public void Scout_Dice_Gen7ou_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "dice" },
                Tiers = new List<string> { "gen5ou" }
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
        public void Scout_New_Format_Other_Winner_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Links = new List<Uri> { new("https://replay.pokemonshowdown.com/smogtours-gen1ou-734539") },
                Users = new List<string> { "RaiZen1704" }
            });

            ClassicAssert.IsTrue(result.Teams.Any());
            ClassicAssert.IsTrue(
                result.Teams.First().Replays.First().Winner == "RaiZen1704"
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
    }
}
