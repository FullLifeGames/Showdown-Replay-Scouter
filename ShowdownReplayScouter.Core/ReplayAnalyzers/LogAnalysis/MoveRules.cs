using System.Collections.Generic;
using System.Linq;
using ShowdownReplayScouter.Core.Data;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    /// <summary>
    /// Adds a used move to a Pokemon, Z-Moves reveal the Z-Crystal instead.
    /// </summary>
    internal sealed class MoveRules(
        IEnumerable<string> illegalMoves,
        IDictionary<string, string> itemTransformingMoves,
        IDictionary<string, string> zStatusMoveItems
    )
    {
        public void Apply(Pokemon pokemon, string move)
        {
            if (move.StartsWith("Z-") && move.Length > 2)
            {
                // Z-powered status moves, e.g. "Z-Metronome"
                move = move[2..];
                if (zStatusMoveItems.TryGetValue(move, out var zCrystal))
                {
                    pokemon.RevealItem(zCrystal);
                }
            }
            if (itemTransformingMoves.TryGetValue(move, out var item))
            {
                pokemon.RevealItem(item);
            }
            else if (!pokemon.Moves.Contains(move) && !illegalMoves.Contains(move))
            {
                pokemon.Moves.Add(move);
            }
        }
    }
}
