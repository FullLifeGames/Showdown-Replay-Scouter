namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    /// <summary>
    /// Parses Pokemon idents like "p1a: Nick" (side "p1", position "a", nickname "Nick").
    /// </summary>
    internal static class PokemonIdent
    {
        public static bool TryParse(string? ident, out string position, out string nickname)
        {
            position = "";
            nickname = "";
            if (ident is null)
            {
                return false;
            }
            var separator = ident.IndexOf(':');
            if (separator <= 0)
            {
                return false;
            }
            position = ident[..separator].Trim();
            nickname = ident[(separator + 1)..].Trim();
            return position.Length is 2 or 3 && position[0] == 'p' && char.IsDigit(position[1]);
        }

        public static bool AreSame(string? first, string? second)
        {
            return TryParse(first, out var firstPosition, out var firstNickname)
                && TryParse(second, out var secondPosition, out var secondNickname)
                && firstPosition == secondPosition
                && firstNickname == secondNickname;
        }
    }
}
