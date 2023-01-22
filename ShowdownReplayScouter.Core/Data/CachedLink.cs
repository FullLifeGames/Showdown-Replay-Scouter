using System;

namespace ShowdownReplayScouter.Core.Data
{
    public class CachedLink
    {
        public Uri ReplayLog { get; set; } = null!;
        public string? Format { get; set; }
    }
}
