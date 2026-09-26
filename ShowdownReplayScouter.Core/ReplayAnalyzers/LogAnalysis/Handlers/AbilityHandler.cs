using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis.Handlers
{
    /// <summary>
    /// Attributes revealed abilities to their holder, based on the "[from]" and "[of]" arguments.
    /// </summary>
    internal sealed class AbilityHandler : ILogLineHandler
    {
        /// <summary>
        /// Abilities that copy the ability of the [of] Pokemon ("|-ability|p1a: Holder|Copied|[from] ability: Trace|[of] p2a: Other").
        /// </summary>
        private static readonly HashSet<string> CopyingAbilities =
        [
            "Trace",
            "Receiver",
            "Power of Alchemy",
        ];

        public void Handle(ProtocolLine line, AnalysisContext context)
        {
            if (!line.IsEffect)
            {
                return;
            }

            var (holder, ability) =
                line.From?.StartsWith("ability:") == true
                    ? FromAbility(line, context)
                    : AbilityArgument(line);
            RevealAbility(context, holder, ability);
        }

        /// <summary>
        /// "[from] ability: X", usually held by the [of] Pokemon.
        /// </summary>
        private static (string? Holder, string Ability) FromAbility(
            ProtocolLine line,
            AnalysisContext context
        )
        {
            var ability = line.From!["ability:".Length..].Trim();
            var of = line.Of;
            switch (line.Command)
            {
                case "-ability" when CopyingAbilities.Contains(ability):
                    RevealAbility(context, of, line.Arg(3));
                    return (line.Main, ability);
                case "-item":
                    // Frisk reveals the item of the main Pokemon, Magician / Pickpocket steal it
                    return (ability == "Frisk" ? of : line.Main, ability);
                case "-heal":
                    // Absorbing abilities list the attacker as [of]
                    return (ability == "Hospitality" ? of : line.Main, ability);
                case "-enditem":
                    return (PokemonIdent.AreSame(of, line.Main) ? null : of, ability);
                default:
                    // e.g. weather, Flame Body, Toxic Chain
                    return (of ?? line.Main, ability);
            }
        }

        /// <summary>
        /// "ability: X" as argument ("|-activate|p1a: Holder|ability: X") or "|-ability|p1a: Holder|X".
        /// </summary>
        private static (string? Holder, string? Ability) AbilityArgument(ProtocolLine line)
        {
            var abilityPart = line
                .Parts.Skip(2)
                .FirstOrDefault((part) => part.StartsWith("ability:"));
            if (abilityPart is not null)
            {
                var ability = abilityPart["ability:".Length..].Trim();
                // "|-block|p1a: Target|ability: Flower Veil|[of] p1b: Holder"
                return (line.Command == "-block" ? line.Of ?? line.Main : line.Main, ability);
            }
            if (line.Command == "-ability" && line.From is null)
            {
                return (line.Main, line.Arg(3));
            }
            return (null, null);
        }

        private static void RevealAbility(AnalysisContext context, string? ident, string? ability)
        {
            ability = ability?.Trim();
            if (string.IsNullOrEmpty(ability))
            {
                return;
            }
            context.Resolve(ident)?.RevealAbility(ability);
        }
    }
}
