using System;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    public interface IReplayCollector
    {
        public IAsyncEnumerable<Uri> CollectReplaysAsync(string user, string tier = null, string opponent = null);
    }
}
