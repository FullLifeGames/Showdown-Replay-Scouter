using System.Text.RegularExpressions;

namespace ShowdownReplayScouter.Core.Util
{
    public static class RegexUtil
    {
        private static readonly Regex _regex = new("[^a-zA-Z0-9]");
        public static string Regex(string toFilter)
        {
            toFilter = _regex.Replace(toFilter, "");
            return toFilter.ToLower();
        }
    }
}
