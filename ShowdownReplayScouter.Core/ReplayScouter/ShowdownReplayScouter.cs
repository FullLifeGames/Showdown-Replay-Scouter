using Microsoft.Extensions.Caching.Distributed;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;

namespace ShowdownReplayScouter.Core.ReplayScouter
{
    public class ShowdownReplayScouter : ReplayScouter
    {
        public ShowdownReplayScouter() : this(null) { }

        private readonly IDistributedCache? _cache;
        public ShowdownReplayScouter(IDistributedCache? cache)
        {
            _cache = cache;
        }

        public override IReplayAnalyzer ReplayAnalyzer => new ShowdownReplayAnalyzer(_cache);

        public override IReplayCollector ReplayCollector => new ShowdownReplayCollector(_cache);

        public override ITeamMerger TeamMerger => new ShowdownTeamMerger();
    }
}
