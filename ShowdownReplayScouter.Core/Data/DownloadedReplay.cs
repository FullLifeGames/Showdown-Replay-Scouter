namespace ShowdownReplayScouter.Core.Data
{
    /// <summary>
    /// A replay as downloaded from Showdown, including its battle log.
    /// The log is only needed to analyze the replay, so teams keep a <see cref="Replay.Clone"/>
    /// of it, which has no log and therefore does not hold on to it.
    /// </summary>
    internal sealed class DownloadedReplay : Replay
    {
        public string Log { get; set; } = null!;
    }
}
