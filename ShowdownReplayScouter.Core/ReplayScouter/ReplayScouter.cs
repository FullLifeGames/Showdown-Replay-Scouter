using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayScouter
{
    public abstract class ReplayScouter
    {
        public abstract IReplayAnalyzer ReplayAnalyzer { get; }
        public abstract IReplayCollector ReplayCollector { get; }
        public abstract ITeamMerger TeamMerger { get; }

        public virtual ScoutingResult? ScoutReplays(ScoutingRequest scoutingRequest)
        {
            return ScoutReplaysAsync(scoutingRequest).Result;
        }

        public virtual async Task<ScoutingResult?> ScoutReplaysAsync(
            ScoutingRequest scoutingRequest
        )
        {
            if (scoutingRequest == null)
            {
                return null;
            }

            var teamCollection = new ConcurrentBag<Team>();

            if (scoutingRequest.Links?.Any() == true)
            {
                await ParallelHelper
                    .ParallelForEachAsync(
                        scoutingRequest.Links,
                        async (replay) =>
                        {
                            if (scoutingRequest.Users?.Any() == true)
                            {
                                foreach (var user in scoutingRequest.Users)
                                {
                                    try
                                    {
                                        await AnalyzeReplayAsync(
                                                new CollectedReplay(replay, user),
                                                teamCollection
                                            )
                                            .ConfigureAwait(false);
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine($"Error on {replay}");
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    await AnalyzeReplayAsync(
                                            new CollectedReplay(replay, null),
                                            teamCollection
                                        )
                                        .ConfigureAwait(false);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine($"Error on {replay}");
                                }
                            }
                        },
                        Environment.ProcessorCount
                    )
                    .ConfigureAwait(false);
            }
            else if (scoutingRequest.Users?.Any() == true || scoutingRequest.Tiers?.Any() == true)
            {
                await ParallelHelper
                    .AsyncParallelForEachAsync(
                        ReplayCollector.CollectReplaysAsync(scoutingRequest),
                        async (collectedReplay) =>
                        {
                            try
                            {
                                await AnalyzeReplayAsync(collectedReplay, teamCollection)
                                    .ConfigureAwait(false);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine($"Error on {collectedReplay}");
                            }
                        },
                        Environment.ProcessorCount
                    )
                    .ConfigureAwait(false);
            }

            return new ScoutingResult() { Teams = TeamMerger.MergeTeams(teamCollection) };
        }

        private async Task AnalyzeReplayAsync(
            CollectedReplay collectedReplay,
            ConcurrentBag<Team> teamCollection
        )
        {
            var teams = await ReplayAnalyzer
                .AnalyzeReplayAsync(collectedReplay.Replay, collectedReplay.User)
                .ConfigureAwait(false);
            foreach (var team in teams)
            {
                teamCollection.Add(team);
            }
        }
    }
}
