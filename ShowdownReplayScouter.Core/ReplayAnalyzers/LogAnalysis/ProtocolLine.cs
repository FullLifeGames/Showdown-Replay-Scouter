using System.Linq;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    /// <summary>
    /// A single line of the battle log, e.g. "|-status|p1a: Nick|brn|[from] ability: Flame Body|[of] p2a: Other".
    /// </summary>
    internal sealed class ProtocolLine
    {
        public ProtocolLine(string line)
        {
            Parts = line.TrimEnd('\r').Split('|');
        }

        public string[] Parts { get; }

        public string Command => Parts.Length > 1 ? Parts[1] : "";

        /// <summary>
        /// The main Pokemon of the message ("p1a: Nick"), side messages ("|-sidestart|p1: Player|...") have none.
        /// </summary>
        public string? Main => Command.StartsWith("-side") ? null : Arg(2);

        /// <summary>
        /// The effect causing the message, e.g. "ability: Flame Body" or "move: Metronome".
        /// </summary>
        public string? From => GetKeywordArgument("[from]");

        /// <summary>
        /// The Pokemon the effect originates from.
        /// </summary>
        public string? Of => GetKeywordArgument("[of]");

        /// <summary>
        /// Minor battle messages ("|-damage|", "|-status|", ...) and "|cant|",
        /// which can reveal abilities and items.
        /// </summary>
        public bool IsEffect => Command.StartsWith('-') || Command == "cant";

        public string? Arg(int index)
        {
            return Parts.Length > index ? Parts[index] : null;
        }

        /// <summary>
        /// All arguments after the command, joined back together (e.g. chat messages containing "|").
        /// </summary>
        public string ArgsFrom(int index)
        {
            return string.Join("|", Parts.Skip(index));
        }

        private string? GetKeywordArgument(string key)
        {
            var part = Parts.Skip(2).FirstOrDefault((part) => part.StartsWith(key));
            return part?[key.Length..].Trim();
        }
    }
}
