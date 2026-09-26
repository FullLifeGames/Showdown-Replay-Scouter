using System;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    internal static class SpeciesForms
    {
        /// <summary>
        /// Species with a hyphen in their name, which are no form of another species
        /// (e.g. "Porygon-Z" is no form of "Porygon").
        /// </summary>
        private static readonly HashSet<string> HyphenatedSpecies =
        [
            "Nidoran-F",
            "Nidoran-M",
            "Ho-Oh",
            "Porygon-Z",
            "Jangmo-o",
            "Hakamo-o",
            "Kommo-o",
            "Wo-Chien",
            "Chien-Pao",
            "Ting-Lu",
            "Chi-Yu"
        ];

        /// <summary>
        /// Whether <paramref name="species"/> is a form of <paramref name="baseSpecies"/>,
        /// e.g. "Urshifu-Rapid-Strike" of "Urshifu" (shown as "Urshifu-*" in team preview).
        /// </summary>
        public static bool IsFormOf(string species, string? baseSpecies)
        {
            return baseSpecies is not null
                && !HyphenatedSpecies.Contains(species)
                && species.StartsWith($"{baseSpecies}-", StringComparison.OrdinalIgnoreCase);
        }
    }
}
