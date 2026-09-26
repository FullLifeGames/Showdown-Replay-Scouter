using System.Net.Http;

namespace ShowdownReplayScouter.Core.Util
{
    public static class Common
    {
        private const string UserAgent =
            "ShowdownReplayScouter (+https://github.com/FullLifeGames/Showdown-Replay-Scouter)";

        private static HttpClient? _httpClient;
        public static HttpClient HttpClient
        {
            get { return _httpClient ??= CreateHttpClient(); }
            set => _httpClient = value;
        }

        /// <summary>
        /// The maximum Levenshtein distance for a player name to still match the searched user,
        /// shorter names accept fewer differences (a quarter of their length).
        /// </summary>
        public static int LevenshteinDistanceAcceptable { get; set; } = 3;

        private static HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient(new HttpRetryMessageHandler(new HttpClientHandler()));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            return httpClient;
        }
    }
}
