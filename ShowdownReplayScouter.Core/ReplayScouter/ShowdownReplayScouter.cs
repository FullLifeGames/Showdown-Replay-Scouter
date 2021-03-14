using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;

namespace ShowdownReplayScouter.Core.ReplayScouter
{
    public class ShowdownReplayScouter : ReplayScouter
    {
        public override IReplayAnalyzer ReplayAnalyzer => new ShowdownReplayAnalyzer();

        public override IReplayCollector ReplayCollector => new ShowdownReplayCollector();

        public override ITeamMerger TeamMerger => new ShowdownTeamMerger();
    }
}
