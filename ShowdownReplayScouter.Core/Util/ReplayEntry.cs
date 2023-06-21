namespace ShowdownReplayScouter.Core.Util
{
    public class ReplayEntry
    {
        public long Uploadtime { get; set; }
        public string Id { get; set; } = null!;
        public string Format { get; set; } = null!;
        public string P1 { get; set; } = null!;
        public string P2 { get; set; } = null!;
    }
}
