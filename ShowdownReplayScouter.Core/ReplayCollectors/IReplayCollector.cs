using System.Collections.Generic;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    public interface IReplayCollector
    {
        public IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(
            ScoutingRequest scoutingRequest
        );
    }
}
