using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    /// <summary>
    /// A set in the Showdown export format, as posted with "!showteam" / "!showset".
    /// </summary>
    internal sealed record ShowdownSet(
        string? Nickname,
        string Species,
        string? Item,
        string? Ability,
        string? TeraType,
        IReadOnlyList<string> Moves
    );
}
