using System;

namespace ShowdownReplayScouter.Core.Data
{
    [Serializable]
    public class Replay
    {
        public string Id { get; set; } = null!;
        public string P1 { get; set; } = null!;
        public string P2 { get; set; } = null!;
        public string Format { get; set; } = null!;
        public string Log { internal get; set; } = null!;
        public int UploadTime { get; set; }
        public int Views { get; set; }
        public string P1Id { get; set; } = null!;
        public string P2Id { get; set; } = null!;
        public string FormatId { get; set; } = null!;
        public int Rating { get; set; }
        public int Private { get; set; }
        public string Password { get; set; } = null!;
        /// <summary>
        /// If a winner exists, it is present. If not or tie => null
        /// </summary>
        public string? Winner { get; set; }
        public bool WinForTeam { get; set; }
        public PlayerInfo PlayerInfo { get; set; } = null!;
        public Uri Link { get; set; } = null!;

        public Replay Clone()
        {
            return new Replay
            {
                Id = Id,
                P1 = P1,
                P2 = P2,
                Format = Format,
                Log = Log,
                UploadTime = UploadTime,
                Views = Views,
                P1Id = P1Id,
                P2Id = P2Id,
                FormatId = FormatId,
                Rating = Rating,
                Private = Private,
                Password = Password,
                Winner = Winner,
                WinForTeam = WinForTeam,
                PlayerInfo = PlayerInfo,
                Link = Link
            };
        }
    }
}
