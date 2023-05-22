using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    public interface IReplayCollector
    {
        public IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(ScoutingRequest scoutingRequest);
    }
}
