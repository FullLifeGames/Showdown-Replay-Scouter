using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public virtual Task<ScoutingResult?> ScoutReplaysAsync(ScoutingRequest scoutingRequest)
        {
            return ScoutReplaysAsync(scoutingRequest, CancellationToken.None);
        }

        /// <summary>
        /// Scouts the replays and stops downloading and analyzing them when the token is canceled
        /// (e.g. the client of a request went away), throwing an <see cref="OperationCanceledException"/>.
        /// </summary>
        public virtual async Task<ScoutingResult?> ScoutReplaysAsync(
            ScoutingRequest scoutingRequest,
            CancellationToken cancellationToken
        )
        {
            if (scoutingRequest == null)
            {
                return null;
            }

            var teamCollection = new ConcurrentBag<Team>();
            var maxDegreeOfParallelism = GetMaxDegreeOfParallelism(scoutingRequest);

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
                                                teamCollection,
                                                cancellationToken
                                            )
                                            .ConfigureAwait(false);
                                    }
                                    catch (Exception)
                                        when (!cancellationToken.IsCancellationRequested)
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
                                            teamCollection,
                                            cancellationToken
                                        )
                                        .ConfigureAwait(false);
                                }
                                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                                {
                                    Console.WriteLine($"Error on {replay}");
                                }
                            }
                        },
                        maxDegreeOfParallelism,
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            else if (scoutingRequest.Users?.Any() == true || scoutingRequest.Tiers?.Any() == true)
            {
                await ParallelHelper
                    .AsyncParallelForEachAsync(
                        ReplayCollector.CollectReplaysAsync(scoutingRequest, cancellationToken),
                        async (collectedReplay) =>
                        {
                            try
                            {
                                await AnalyzeReplayAsync(
                                        collectedReplay,
                                        teamCollection,
                                        cancellationToken
                                    )
                                    .ConfigureAwait(false);
                            }
                            catch (Exception) when (!cancellationToken.IsCancellationRequested)
                            {
                                Console.WriteLine($"Error on {collectedReplay}");
                            }
                        },
                        maxDegreeOfParallelism,
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            IEnumerable<Team> teams = teamCollection;
            if (scoutingRequest.Grouped)
            {
                teams = TeamMerger.MergeTeams(teams);
            }

            return new ScoutingResult() { Teams = teams };
        }

        protected virtual int DefaultMaxConcurrentReplayAnalysis =>
            Math.Max(Environment.ProcessorCount, 16);

        private int GetMaxDegreeOfParallelism(ScoutingRequest scoutingRequest)
        {
            if (scoutingRequest.MaxConcurrentReplayAnalysis is > 0)
            {
                return scoutingRequest.MaxConcurrentReplayAnalysis.Value;
            }

            return DefaultMaxConcurrentReplayAnalysis;
        }

        private async Task AnalyzeReplayAsync(
            CollectedReplay collectedReplay,
            ConcurrentBag<Team> teamCollection,
            CancellationToken cancellationToken
        )
        {
            var teams = await ReplayAnalyzer
                .AnalyzeReplayAsync(collectedReplay.Replay, collectedReplay.User, cancellationToken)
                .ConfigureAwait(false);
            foreach (var team in teams)
            {
                teamCollection.Add(team);
            }
        }
    }
}
