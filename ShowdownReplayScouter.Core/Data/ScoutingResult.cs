using System;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.Data
{
    [Serializable]
    public class ScoutingResult
    {
        public IEnumerable<Team> Teams = null!;
    }
}
