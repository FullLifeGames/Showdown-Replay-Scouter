namespace ShowdownReplayScouter.Core.Data
{
    public class Replay
    {
        public string Id { get; set; } = null!;
        public string P1 { get; set; } = null!;
        public string P2 { get; set; } = null!;
        public string Format { get; set; } = null!;
        public string Log { get; set; } = null!;
        public int UploadTime { get; set; }
        public int Views { get; set; }
        public string P1Id { get; set; } = null!;
        public string P2Id { get; set; } = null!;
        public string FormatId { get; set; } = null!;
        public int Rating { get; set; }
        public int Private { get; set; }
        public string Password { get; set; } = null!;
    }
}
