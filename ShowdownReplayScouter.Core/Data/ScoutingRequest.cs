using System;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.Data
{
    public class ScoutingRequest
    {
        public string User { get; set; }
        public string Tier { get; set; }
        public string Opponent { get; set; }
        public IEnumerable<Uri> Links { get; set; }
    }
}
