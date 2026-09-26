using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    /// <summary>
    /// Adds revealed information to a Pokemon, keeping every distinct alternative ("Leftovers | Life Orb").
    /// </summary>
    internal static class PokemonRevealExtensions
    {
        public static void RevealAbility(this Pokemon pokemon, string ability)
        {
            pokemon.Ability = Common.MergeAlternatives(pokemon.Ability, ability);
        }

        public static void RevealItem(this Pokemon pokemon, string? item)
        {
            pokemon.Item = Common.MergeAlternatives(pokemon.Item, item);
        }

        public static void RevealTeraType(this Pokemon pokemon, string teraType)
        {
            pokemon.TeraType = Common.MergeAlternatives(pokemon.TeraType, teraType);
        }
    }
}
