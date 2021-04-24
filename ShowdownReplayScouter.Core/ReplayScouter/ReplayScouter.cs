using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using ShowdownReplayScouter.Core.Util;

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
            var runningTasks = new ConcurrentBag<Task>();
            var running = new bool[] { true };

            InitCollectionTask(runningTasks, running);

            if (scoutingRequest.Links != null && scoutingRequest.Links.Any())
            {
                foreach (var replay in scoutingRequest.Links)
                {
                    AddAnalyzingTask(scoutingRequest, teamCollection, runningTasks, replay);
                }
            }
            else if (scoutingRequest.User != null)
            {
                await foreach (var replay in ReplayCollector.CollectReplaysAsync(scoutingRequest.User, scoutingRequest.Tier, scoutingRequest.Opponent))
                {
                    AddAnalyzingTask(scoutingRequest, teamCollection, runningTasks, replay);
                }
            }

            WaitForCompletionAndStopCollectionTask(runningTasks, running);

            return new ScoutingResult()
            {
                Teams = TeamMerger.MergeTeams(teamCollection)
            };
        }

        private static void InitCollectionTask(ConcurrentBag<Task> runningTasks, bool[] running)
        {
            Task.Run(() =>
            {
                while (running[0])
                {
                    var runningTasksCount = runningTasks.Where((task) => task.Status == TaskStatus.Running).Count();
                    if (runningTasksCount < Common.NumberOfTasks)
                    {
                        foreach (var runTask in runningTasks.Where((task) => task.Status == TaskStatus.Created).Take(Common.NumberOfTasks - runningTasksCount))
                        {
                            runTask.Start();
                        }
                    }
                }
            });
        }

        private void AddAnalyzingTask(ScoutingRequest scoutingRequest, ConcurrentBag<Team> teamCollection, ConcurrentBag<Task> runningTasks, System.Uri replay)
        {
            runningTasks.Add(new Task(() =>
            {
                foreach (var team in ReplayAnalyzer.AnalyzeReplayAsync(replay, scoutingRequest.User).Result)
                {
                    teamCollection.Add(team);
                }
            }));
        }

        private static void WaitForCompletionAndStopCollectionTask(ConcurrentBag<Task> runningTasks, bool[] running)
        {
            while (runningTasks.Any((task) => !task.IsCompleted));
            running[0] = false;
        }
    }
}
