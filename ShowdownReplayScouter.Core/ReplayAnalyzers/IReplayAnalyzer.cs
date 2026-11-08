using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShowdownReplayScouter.Core.Data;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers
{
    public interface IReplayAnalyzer
    {
        public IEnumerable<Team> AnalyzeReplay(string replay);
        public Task<IEnumerable<Team>> AnalyzeReplayAsync(string replay);
        public IEnumerable<Team> AnalyzeReplay(Uri replay);
        public Task<IEnumerable<Team>> AnalyzeReplayAsync(Uri replay);
        public IEnumerable<Team> AnalyzeReplay(string replay, string? user);
        public Task<IEnumerable<Team>> AnalyzeReplayAsync(string replay, string? user);
        public IEnumerable<Team> AnalyzeReplay(Uri replay, string? user);
        public Task<IEnumerable<Team>> AnalyzeReplayAsync(Uri replay, string? user);

        /// <summary>
        /// Like <see cref="AnalyzeReplayAsync(Uri, string?)"/>, but stops when the token is canceled.
        /// Implementations that do not override this ignore the token.
        /// </summary>
        public Task<IEnumerable<Team>> AnalyzeReplayAsync(
            Uri replay,
            string? user,
            CancellationToken cancellationToken
        ) => AnalyzeReplayAsync(replay, user);
    }
}
