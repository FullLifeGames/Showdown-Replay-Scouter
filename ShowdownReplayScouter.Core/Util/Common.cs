using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ShowdownReplayScouter.Core.Util
{
    public static class Common
    {
        private static HttpClient? _httpClient;
        public static HttpClient HttpClient
        {
            get
            {
                return _httpClient ??= new HttpClient(
                    new HttpRetryMessageHandler(new HttpClientHandler())
                );
            }
            set => _httpClient = value;
        }

        public static int LevenshteinDistanceAcceptable { get; set; } = 3;

        public static IEnumerable<string> FormPokemonList { get; set; } =
            ["Arceus", "Silvally", "Genesect", "Gourgeist", "Pumpkaboo"];

        public const string AlternativeSeparator = " | ";

        /// <summary>
        /// Merges two " | " separated lists of alternatives (e.g. items or tera types),
        /// keeping the order of first occurrence and dropping duplicates.
        /// </summary>
        public static string? MergeAlternatives(string? existing, string? addition)
        {
            if (string.IsNullOrEmpty(addition))
            {
                return existing;
            }
            if (string.IsNullOrEmpty(existing))
            {
                return addition;
            }
            var values = existing.Split(AlternativeSeparator).ToList();
            foreach (var value in addition.Split(AlternativeSeparator))
            {
                if (!values.Contains(value))
                {
                    values.Add(value);
                }
            }
            return string.Join(AlternativeSeparator, values);
        }
    }
}
