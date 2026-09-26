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
    }
}
