using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis.Handlers;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    /// <summary>
    /// Builds the team of one player from a replay log by passing every line to the handlers.
    /// </summary>
    internal static class ReplayLogAnalyzer
    {
        public static void Analyze(
            string? user,
            PlayerInfo playerInfo,
            Team team,
            string replayLog,
            Replay? replay,
            MoveRules moveRules
        )
        {
            var context = new AnalysisContext(team, playerInfo, replay, moveRules);
            // The team handler runs first, so the others can resolve switched in Pokemon
            ILogLineHandler[] handlers =
            [
                new TeamHandler(),
                new MoveHandler(),
                new ItemHandler(),
                new AbilityHandler(),
                new PostedSetHandler(),
                new WinHandler(),
            ];

            foreach (var rawLine in replayLog.Split('\n'))
            {
                var line = new ProtocolLine(rawLine);
                if (line.Parts.Length < 2)
                {
                    continue;
                }
                if (line.Command == "player")
                {
                    PlayerDetector.DeterminePlayer(user, playerInfo, line.Parts);
                    continue;
                }
                if (playerInfo.PlayerValue?.Length == 0)
                {
                    continue;
                }

                foreach (var handler in handlers)
                {
                    handler.Handle(line, context);
                }
            }
        }
    }
}
