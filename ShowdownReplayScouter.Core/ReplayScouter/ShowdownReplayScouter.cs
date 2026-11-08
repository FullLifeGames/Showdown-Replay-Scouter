using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers;
using ShowdownReplayScouter.Core.ReplayCollectors;
using ShowdownReplayScouter.Core.TeamMergers;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayScouter
{
    public class ShowdownReplayScouter : ReplayScouter
    {
        public ShowdownReplayScouter()
            : this(null) { }

        private readonly CacheCollector? _cache;
        private readonly IReplayAnalyzer _replayAnalyzer;
        private readonly IReplayCollector _replayCollector;
        private readonly ITeamMerger _teamMerger;

        public ShowdownReplayScouter(IDistributedCache? cache)
        {
            // Without a backing cache, a CacheCollector would only buffer every write of the
            // request (including a per-player replay list rewritten for each replay) and then
            // discard it all in Store().
            _cache = cache is null ? null : new CacheCollector(cache);
            _replayAnalyzer = new ShowdownReplayAnalyzer(_cache);
            _replayCollector = new ApiShowdownReplayCollector(_cache);
            _teamMerger = new ShowdownTeamMerger();
        }

        public override IReplayAnalyzer ReplayAnalyzer => _replayAnalyzer;

        public override IReplayCollector ReplayCollector => _replayCollector;

        public override ITeamMerger TeamMerger => _teamMerger;

        public override ScoutingResult? ScoutReplays(ScoutingRequest scoutingRequest)
        {
            var result = base.ScoutReplays(scoutingRequest);
            _cache?.Store();
            return result;
        }

        public override async Task<ScoutingResult?> ScoutReplaysAsync(
            ScoutingRequest scoutingRequest
        )
        {
            var result = await base.ScoutReplaysAsync(scoutingRequest);
            _cache?.Store();
            return result;
        }
    }
}
