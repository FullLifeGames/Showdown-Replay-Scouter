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
        public void Scout_FullLifeGames_MultipleTiers_Replays()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                Users = new List<string> { "fulllifegames" },
                Tiers = new List<string> { "gen7ou", "gen8ou" }
            });

            Assert.IsTrue(
                result.Teams.Any((team) => team.Links.Any((link) => link.ToString().Contains("gen7ou")))
                &&
                result.Teams.Any((team) => team.Links.Any((link) => link.ToString().Contains("gen8ou")))
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
    }
}
