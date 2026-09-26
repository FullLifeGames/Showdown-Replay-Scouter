using System;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis.Handlers
{
    /// <summary>
    /// Records the winner of the replay and whether the scouted player won.
    /// </summary>
    internal sealed class WinHandler : ILogLineHandler
    {
        public void Handle(ProtocolLine line, AnalysisContext context)
        {
            var winner = line.Arg(2);
            var replay = context.Replay;
            if (line.Command != "win" || replay is null || winner is null)
            {
                return;
            }
            replay.Winner = winner;
            var regexWinner = RegexUtil.Regex(replay.Winner).ToLower();
            replay.WinForTeam = regexWinner.Equals(
                RegexUtil.Regex(context.PlayerInfo.PlayerName),
                StringComparison.CurrentCultureIgnoreCase
            );
        }
    }
}
