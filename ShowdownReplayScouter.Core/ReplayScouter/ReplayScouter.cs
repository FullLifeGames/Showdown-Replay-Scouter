using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ShowdownReplayScouter.Core.ReplayScouter
{
    public abstract class ReplayScouter
    {
        public abstract IReplayAnalyzer ReplayAnalyzer
        {
            get;
        }
        public abstract IReplayCollector ReplayCollector
        {
            get;
        }
        public abstract ITeamMerger TeamMerger
        {
            get;
        }

        public ScoutingResult ScoutReplays(ScoutingRequest scoutingRequest)
        {
            return ScoutReplaysAsync(scoutingRequest).Result;
        }

        public async Task<ScoutingResult> ScoutReplaysAsync(ScoutingRequest scoutingRequest)
        {
            if (scoutingRequest == null)
            {
                return null;
            }

            var teamCollection = new ConcurrentBag<Team>();

            if (scoutingRequest.Links?.Any() == true)
            {
                await Parallel.ForEachAsync(scoutingRequest.Links, async (replay, _)
                    => await AnalyzeReplayAsync(scoutingRequest, teamCollection, replay)
                        .ConfigureAwait(false))
                    .ConfigureAwait(false);
            }
            else if (scoutingRequest.User != null)
            {
                await Parallel.ForEachAsync(
                    ReplayCollector.CollectReplaysAsync(scoutingRequest.User, scoutingRequest.Tier, scoutingRequest.Opponent),
                    async (replay, _) =>
                        await AnalyzeReplayAsync(scoutingRequest, teamCollection, replay)
                        .ConfigureAwait(false)
                ).ConfigureAwait(false);
            }

            return new ScoutingResult()
            {
                Teams = TeamMerger.MergeTeams(teamCollection)
            };
        }

        private async Task AnalyzeReplayAsync(ScoutingRequest scoutingRequest, ConcurrentBag<Team> teamCollection, Uri replay)
        {
            var teams = await ReplayAnalyzer.AnalyzeReplayAsync(replay, scoutingRequest.User).ConfigureAwait(false);
            foreach (var team in teams)
            {
                teamCollection.Add(team);
            }
        }
    }
}
