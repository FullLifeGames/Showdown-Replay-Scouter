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
                User = "fulllifegames",
                Tier = "gen7ou"
            });

            Assert.IsTrue(result.Teams.Count() > 0);
        }

        [Test]
        public void Scout_FullLifeGames_Single_Gen7Ou_Form_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                User = "fulllifegames",
                Links = new List<Uri>()
                {
                    new Uri("https://replay.pokemonshowdown.com/future-gen7ou-2227")
                }
            });

            Assert.IsTrue(result.Teams.Count() > 0 && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_FullLifeGames_Single_Gen7Ou_Nickname_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                User = "fulllifegames",
                Links = new List<Uri>()
                {
                    new Uri("http://replay.pokemonshowdown.com/smogtours-gen7ou-450650")
                }
            });

            Assert.IsTrue(result.Teams.Count() > 0 && result.Teams.First().Pokemon.Count == 6);
        }

        [Test]
        public void Scout_FullLifeGames_Single_Gen7Ubers_Form_Replay()
        {
            var result = _replayScouter.ScoutReplays(new Core.Data.ScoutingRequest()
            {
                User = "fulllifegames",
                Links = new List<Uri>()
                {
                    new Uri("https://replay.pokemonshowdown.com/gen7pokebankubers-499462699")
                }
            });

            Assert.IsTrue(result.Teams.Count() > 0 && result.Teams.First().Pokemon.Count == 6);
        }
    }
}
