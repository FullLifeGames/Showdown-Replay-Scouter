﻿namespace ShowdownReplayScouter.Core.Util
{
    public class ReplayEntry
    {
        public long Uploadtime { get; set; }
        public string Id { get; set; } = null!;
        public string Format { get; set; } = null!;
        public string[] Players { get; set; } = null!;
    }
}
