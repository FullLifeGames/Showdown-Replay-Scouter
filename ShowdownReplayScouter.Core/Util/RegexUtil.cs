using System.Text.RegularExpressions;

namespace ShowdownReplayScouter.Core.Util
{
    public class RegexUtil
    {
        private static Regex _rgx = new Regex("[^a-zA-Z0-9]");
        public static string Regex(string toFilter)
        {
            toFilter = _rgx.Replace(toFilter, "");
            return toFilter.ToLower();
        }
    }
}
