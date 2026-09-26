using System;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    internal static class PlayerDetector
    {
        /// <summary>
        /// Determines from a "|player|p1|Name|..." line whether this is the searched user.
        /// </summary>
        public static void DeterminePlayer(
            string? rawUser,
            PlayerInfo playerInfo,
            string[] playerinf
        )
        {
            var setPlayer = false;
            var regexedPlayerInf = "";
            if (playerinf.Length > 3)
            {
                regexedPlayerInf = RegexUtil.Regex(playerinf[3].ToLower());
            }

            if (rawUser != null)
            {
                var user = RegexUtil.Regex(rawUser.ToLower());
                if (playerinf.Length > 3)
                {
                    var distance = LevenshteinDistance.Compute(regexedPlayerInf, user);
                    if (regexedPlayerInf.Contains(user))
                    {
                        if (!string.IsNullOrWhiteSpace(playerInfo.PlayerName))
                        {
                            if (playerInfo.PlayerName.Contains(user))
                            {
                                if (
                                    LevenshteinDistance.Compute(playerInfo.PlayerName, user)
                                    > distance
                                )
                                {
                                    setPlayer = true;
                                }
                            }
                            else
                            {
                                setPlayer = true;
                            }
                        }
                        else
                        {
                            setPlayer = true;
                        }
                    }
                    else if (distance <= AcceptableDistance(user))
                    {
                        if (!string.IsNullOrWhiteSpace(playerInfo.PlayerName))
                        {
                            if (
                                LevenshteinDistance.Compute(playerInfo.PlayerName, user) > distance
                                && !playerInfo.PlayerName.Contains(user)
                            )
                            {
                                setPlayer = true;
                            }
                        }
                        else
                        {
                            setPlayer = true;
                        }
                    }
                }
            }

            if (setPlayer)
            {
                playerInfo.PlayerValue = playerinf[2];
                playerInfo.PlayerName = regexedPlayerInf;
            }

            if (playerInfo.PlayerValue == playerinf[2] && playerInfo.PlayerName?.Length == 0)
            {
                playerInfo.PlayerName = regexedPlayerInf;
            }
        }

        /// <summary>
        /// Typos are accepted relative to the length of the name,
        /// so a short name like "bob" does not match "tom".
        /// </summary>
        private static int AcceptableDistance(string user)
        {
            return Math.Min(Common.LevenshteinDistanceAcceptable, user.Length / 4);
        }
    }
}
