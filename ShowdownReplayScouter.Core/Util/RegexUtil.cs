﻿using System.Text.RegularExpressions;

namespace ShowdownReplayScouter.Core.Util
{
    public static partial class RegexUtil
    {
        private static readonly Regex _regex = AlphaNumRegex();

        /// <summary>
        /// Removes any regular expression matches from the input string and returns the filtered string in lowercase.
        /// </summary>
        /// <param name="toFilter">The string to filter.</param>
        /// <returns>The filtered string in lowercase.</returns>
        public static string Regex(string? toFilter)
        {
            if (toFilter is null)
            {
                return "";
            }
            toFilter = _regex.Replace(toFilter, "");
            return toFilter.ToLower();
        }

        [GeneratedRegex("[^a-zA-Z0-9]")]
        private static partial Regex AlphaNumRegex();
    }
}
