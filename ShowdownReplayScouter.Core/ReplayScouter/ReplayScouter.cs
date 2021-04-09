using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

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
            if (scoutingRequest.Links != null && scoutingRequest.Links.Any())
            {
                foreach (var link in scoutingRequest.Links)
                {
                    foreach (var team in await ReplayAnalyzer.AnalyzeReplayAsync(link, scoutingRequest.User))
                    {
                        teamCollection.Add(team);
                    }
                }
            }
            else if (scoutingRequest.User != null)
            {
                await foreach (var replay in ReplayCollector.CollectReplaysAsync(scoutingRequest.User, scoutingRequest.Tier, scoutingRequest.Opponent))
                {
                    foreach (var team in await ReplayAnalyzer.AnalyzeReplayAsync(replay, scoutingRequest.User))
                    {
                        teamCollection.Add(team);
                    }
                }
            }
            return new ScoutingResult()
            {
                Teams = TeamMerger.MergeTeams(teamCollection)
            };
        }
    }
}
