using System;
using System.Collections.Generic;
using System.Net.Http;

namespace ShowdownReplayScouter.Core.Util
{
    public static class Common
    {
        private static HttpClient _httpClient;
        public static HttpClient HttpClient
        {
            get
            {
                return _httpClient ??= new HttpClient();
            }
            set => _httpClient = value;
        }

        public static int LevenshteinDistanceAcceptable { get; set; } = 3;

        public static IEnumerable<string> FormPokemonList { get; set; } = new List<string>
        {
            "Arceus",
            "Silvally",
            "Genesect",
            "Gourgeist",
            "Pumpkaboo"
        };

        public static IEnumerable<string> FormDescriptorList { get; set; } = new List<string>
        {
            "Mega",
            "Origin",
            "Alola",
            "Galar"
        };

        public static IEnumerable<string> OfAbilities { get; set; } = new List<string>
        {
            "Frisk", "Poison Touch",
            "Electric Surge", "Psychic Surge", "Grassy Surge", "Misty Surge",
            "Drought", "Sand Stream", "Drizzle", "Snow Warning"
        };
    }
}
