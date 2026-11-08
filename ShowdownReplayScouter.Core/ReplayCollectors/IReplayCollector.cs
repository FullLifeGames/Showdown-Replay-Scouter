using System.Collections.Generic;
using System.Threading;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    public interface IReplayCollector
    {
        public IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(
            ScoutingRequest scoutingRequest
        );

        /// <summary>
        /// Like <see cref="CollectReplaysAsync(ScoutingRequest)"/>, but stops when the token is canceled.
        /// Implementations that do not override this ignore the token.
        /// </summary>
        public IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(
            ScoutingRequest scoutingRequest,
            CancellationToken cancellationToken
        ) => CollectReplaysAsync(scoutingRequest);
    }
}
