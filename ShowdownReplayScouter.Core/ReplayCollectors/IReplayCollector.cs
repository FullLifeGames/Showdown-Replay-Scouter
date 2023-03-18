using ShowdownReplayScouter.Core.Util;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    public interface IReplayCollector
    {
        public IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(IEnumerable<string>? users = null, IEnumerable<string>? tiers = null, IEnumerable<string>? opponents = null, IEnumerable<string>? searchQueries = null);
    }
}
