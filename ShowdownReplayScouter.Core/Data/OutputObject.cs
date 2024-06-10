using System;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.Data
{
    [Serializable]
    public class OutputObject
    {
        public string Header { get; set; } = "";
        public IList<string> Teams { get; set; } = [];
    }
}
