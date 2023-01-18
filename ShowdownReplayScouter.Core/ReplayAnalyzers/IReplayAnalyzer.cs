using ShowdownReplayScouter.Core.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}
