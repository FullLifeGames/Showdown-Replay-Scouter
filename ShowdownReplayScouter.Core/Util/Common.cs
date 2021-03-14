using System.Collections.Generic;
using System.Net.Http;

namespace ShowdownReplayScouter.Core.Util
{
    public static class Common
    {
        private static HttpClient httpClient;
        public static HttpClient HttpClient
        {
            get
            {
                if (httpClient == null)
                {
                    httpClient = new HttpClient();
                }
                return httpClient;
            }
            set
            {
                httpClient = value;
            }
        }

        public static int LEVENSHTEIN_DISTANCE_ACCEPTABLE { get; set; } = 3;

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

        public static IEnumerable<string> OfAbilities = new List<string>
        {
            "Frisk", "Poison Touch",
            "Electric Surge", "Psychic Surge", "Grassy Surge", "Misty Surge",
            "Drought", "Sand Stream", "Drizzle", "Snow Warning"
        };

    }
}
