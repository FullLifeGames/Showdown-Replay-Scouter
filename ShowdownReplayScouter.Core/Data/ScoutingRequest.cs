using System;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.Data
{
    public class ScoutingRequest
    {
        public IEnumerable<string> Users { get; set; }
        public IEnumerable<string> Tiers { get; set; }
        public IEnumerable<string> Opponents { get; set; }
        public IEnumerable<Uri> Links { get; set; }
    }
}
