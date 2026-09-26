using System.Collections.Generic;
using ShowdownReplayScouter.Core.Data;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis.Handlers
{
    /// <summary>
    /// Tracks the moves of the set: used moves and moves revealed by "cant" or Forewarn.
    /// </summary>
    internal sealed class MoveHandler : ILogLineHandler
    {
        /// <summary>
        /// Moves that call a move which is not part of the set (in contrast to e.g. Sleep Talk).
        /// </summary>
        private static readonly HashSet<string> MoveCallingMoves =
        [
            "Metronome",
            "Assist",
            "Copycat",
            "Mirror Move",
            "Me First",
            "Magic Coat",
            "Nature Power",
            "Snatch",
        ];

        /// <summary>
        /// Pokemon transformed into another Pokemon (Transform / Imposter), their moves are copied.
        /// </summary>
        private readonly HashSet<Pokemon> _transformed = [];

        public void Handle(ProtocolLine line, AnalysisContext context)
        {
            switch (line.Command)
            {
                case "move":
                    HandleMove(line, context);
                    break;
                case "switch":
                case "drag":
                case "replace":
                    var switchedIn = context.Find(line.Arg(2));
                    if (switchedIn is not null)
                    {
                        _transformed.Remove(switchedIn);
                    }
                    break;
                case "-transform":
                    var transformed = context.Resolve(line.Main);
                    if (transformed is not null)
                    {
                        _transformed.Add(transformed);
                    }
                    break;
                case "cant":
                    // "|cant|p1a: Nick|move: Taunt|Calm Mind", with Dazzling / Queenly Majesty
                    // "|cant|p1a: Holder|ability: Queenly Majesty|Extreme Speed|[of] p2a: Nick"
                    var preventedMove = line.Arg(4);
                    if (!string.IsNullOrWhiteSpace(preventedMove) && !preventedMove.StartsWith('['))
                    {
                        RevealMove(context, line.Of ?? line.Main, preventedMove);
                    }
                    break;
                case "-activate":
                    // "|-activate|p1a: Holder|ability: Forewarn|Move|[of] p2a: Nick"
                    if (line.Arg(3) == "ability: Forewarn")
                    {
                        RevealMove(context, line.Of, line.Arg(4));
                    }
                    break;
            }
        }

        private void HandleMove(ProtocolLine line, AnalysisContext context)
        {
            var move = line.Arg(3);
            if (move is null)
            {
                return;
            }
            var pokemon = context.Resolve(line.Arg(2));
            if (pokemon is null)
            {
                return;
            }

            var from = line.From;
            if (from is not null)
            {
                if (from.StartsWith("ability:"))
                {
                    // Moves used by an ability (e.g. Magic Bounce, Dancer) are not part of the set
                    pokemon.RevealAbility(from["ability:".Length..].Trim());
                    return;
                }
                if (from.Contains("Magic Bounce"))
                {
                    pokemon.RevealAbility("Magic Bounce");
                    return;
                }
                var source = from.StartsWith("move:") ? from["move:".Length..].Trim() : from;
                if (MoveCallingMoves.Contains(source))
                {
                    // Only e.g. Metronome itself is part of the set, not the move it called
                    return;
                }
            }
            if (move.StartsWith("Max ") || move.StartsWith("G-Max "))
            {
                // Max Moves do not reveal the move they are based on
                return;
            }

            RevealMove(context, pokemon, move);
        }

        private void RevealMove(AnalysisContext context, string? ident, string? move)
        {
            move = move?.Trim();
            var pokemon = context.Resolve(ident);
            if (pokemon is not null && !string.IsNullOrEmpty(move))
            {
                RevealMove(context, pokemon, move);
            }
        }

        private void RevealMove(AnalysisContext context, Pokemon pokemon, string move)
        {
            if (_transformed.Contains(pokemon))
            {
                // Transformed Pokemon use the moves of the Pokemon they transformed into
                return;
            }
            context.MoveRules.Apply(pokemon, move);
        }
    }
}
