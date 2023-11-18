using Microsoft.Extensions.Caching.Distributed;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;
using ShowdownReplayScouter.Core.Util;
using System.Threading.Tasks;

namespace ShowdownReplayScouter.Core.ReplayScouter
{
    public class ShowdownReplayScouter(IDistributedCache? cache) : ReplayScouter
    {
        public ShowdownReplayScouter() : this(null) { }

        private readonly CacheCollector _cache = new(cache);

        public override IReplayAnalyzer ReplayAnalyzer => new ShowdownReplayAnalyzer(_cache);

        public override IReplayCollector ReplayCollector => new ApiShowdownReplayCollector(_cache);

        public override ITeamMerger TeamMerger => new ShowdownTeamMerger();

        public override ScoutingResult? ScoutReplays(ScoutingRequest scoutingRequest)
        {
            var result = base.ScoutReplays(scoutingRequest);
            _cache.Store();
            return result;
        }

        public async override Task<ScoutingResult?> ScoutReplaysAsync(ScoutingRequest scoutingRequest)
        {
            var result = await base.ScoutReplaysAsync(scoutingRequest);
            _cache.Store();
            return result;
        }
    }
}
