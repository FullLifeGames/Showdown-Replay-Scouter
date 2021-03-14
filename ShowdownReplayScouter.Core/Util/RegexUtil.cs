using System.Text.RegularExpressions;

namespace ShowdownReplayScouter.Core.Util
{
    public class RegexUtil
    {
        private static Regex rgx = new Regex("[^a-zA-Z0-9]");
        public static string Regex(string toFilter)
        {
            toFilter = rgx.Replace(toFilter, "");
            return toFilter.ToLower();
        }

        private static Regex rgxWithSpace = new Regex("[^a-zA-Z0-9 ]");
        private static string RegexWithSpace(string toFilter)
        {
            toFilter = rgxWithSpace.Replace(toFilter, "");
            return toFilter.ToLower();
        }
    }
}
