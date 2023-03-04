using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;
using ShowdownReplayScouter.Core.Util;
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

        public ScoutingResult? ScoutReplays(ScoutingRequest scoutingRequest)
        {
            return ScoutReplaysAsync(scoutingRequest).Result;
        }

        public async Task<ScoutingResult?> ScoutReplaysAsync(ScoutingRequest scoutingRequest)
        {
            if (scoutingRequest == null)
            {
                return null;
            }

            var teamCollection = new ConcurrentBag<Team>();

            if (scoutingRequest.Links?.Any() == true)
            {
                await Parallel.ForEachAsync(scoutingRequest.Links, async (replay, _) =>
                    {
                        if (scoutingRequest.Users?.Any() != true)
                        {
                            try
                            {
                                await AnalyzeReplayAsync(new CollectedReplay(replay, null), teamCollection).ConfigureAwait(false);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine($"Error on {replay}");
                            }
                        }
                        else
                        {
                            foreach (var user in scoutingRequest.Users)
                            {
                                try
                                {
                                    await AnalyzeReplayAsync(new CollectedReplay(replay, user), teamCollection).ConfigureAwait(false);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine($"Error on {replay}");
                                }
                            }
                        }
                    }
                ).ConfigureAwait(false);
            }
            else if (scoutingRequest.Users?.Any() == true || scoutingRequest.Tiers?.Any() == true)
            {
                await Parallel.ForEachAsync(
                    ReplayCollector.CollectReplaysAsync(scoutingRequest.Users, scoutingRequest.Tiers, scoutingRequest.Opponents),
                    async (collectedReplay, _) =>
                        await AnalyzeReplayAsync(collectedReplay, teamCollection)
                        .ConfigureAwait(false)
                ).ConfigureAwait(false);
            }

            return new ScoutingResult()
            {
                Teams = TeamMerger.MergeTeams(teamCollection)
            };
        }

        private async Task AnalyzeReplayAsync(CollectedReplay collectedReplay, ConcurrentBag<Team> teamCollection)
        {
            var teams = await ReplayAnalyzer.AnalyzeReplayAsync(collectedReplay.Replay, collectedReplay.User).ConfigureAwait(false);
            foreach (var team in teams)
            {
                teamCollection.Add(team);
            }
        }
    }
}
