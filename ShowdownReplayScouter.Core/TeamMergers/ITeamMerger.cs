using ShowdownReplayScouter.Core.Data;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.TeamMergers
{
    public interface ITeamMerger
    {
        public IEnumerable<Team> MergeTeams(IEnumerable<Team> teams);
        public Team? MergeTeams(Team? team1, Team? team2);
    }
}
