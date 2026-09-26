using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers
{
    public class ShowdownReplayAnalyzer : IReplayAnalyzer
    {
        public ShowdownReplayAnalyzer()
            : this(null) { }

        private readonly IDistributedCache? _cache;

        public ShowdownReplayAnalyzer(IDistributedCache? cache)
        {
            _cache = cache;
        }

        public IEnumerable<Team> AnalyzeReplay(string replay)
        {
            return AnalyzeReplayAsync(replay).Result;
        }

        public IEnumerable<Team> AnalyzeReplay(Uri replay)
        {
            return AnalyzeReplayAsync(replay).Result;
        }

        public IEnumerable<Team> AnalyzeReplay(string replay, string? user)
        {
            return AnalyzeReplayAsync(replay, user).Result;
        }

        public IEnumerable<Team> AnalyzeReplay(Uri replay, string? user)
        {
            return AnalyzeReplayAsync(replay, user).Result;
        }

        public async Task<IEnumerable<Team>> AnalyzeReplayAsync(string replay)
        {
            return await AnalyzeReplayAsync(new Uri(replay)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Team>> AnalyzeReplayAsync(Uri replay)
        {
            var jsonLink = GetJsonLink(replay);
            var cachedPlayerOneTeam = await GetFromCache(null, "p1", jsonLink)
                .ConfigureAwait(false);
            var cachedPlayerTwoTeam = await GetFromCache(null, "p2", jsonLink)
                .ConfigureAwait(false);
            if (cachedPlayerOneTeam is not null && cachedPlayerTwoTeam is not null)
            {
                return [cachedPlayerOneTeam, cachedPlayerTwoTeam];
            }

            var replayObject = await GetReplayFromUrl(jsonLink).ConfigureAwait(false);
            if (replayObject is null)
            {
                return [cachedPlayerOneTeam ?? new Team(), cachedPlayerTwoTeam ?? new Team()];
            }

            return
            [
                cachedPlayerOneTeam
                    ?? await GetTeamFromReplay(replay, null, "p1", jsonLink, replayObject)
                        .ConfigureAwait(false),
                cachedPlayerTwoTeam
                    ?? await GetTeamFromReplay(replay, null, "p2", jsonLink, replayObject)
                        .ConfigureAwait(false),
            ];
        }

        public async Task<IEnumerable<Team>> AnalyzeReplayAsync(string replay, string? user)
        {
            return await AnalyzeReplayAsync(new Uri(replay), user).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Team>> AnalyzeReplayAsync(Uri replay, string? user)
        {
            if (user == null)
            {
                return await AnalyzeReplayAsync(replay).ConfigureAwait(false);
            }
            return [await GetTeamFromUrl(replay, user).ConfigureAwait(false)];
        }

        private async Task<Team> GetTeamFromUrl(
            Uri link,
            string? user = null,
            string playerValue = ""
        )
        {
            var jsonLink = GetJsonLink(link);

            var cachedTeam = await GetFromCache(user, playerValue, jsonLink).ConfigureAwait(false);
            if (cachedTeam != null)
            {
                return cachedTeam;
            }

            var replayObject = await GetReplayFromUrl(jsonLink).ConfigureAwait(false);
            if (replayObject is null)
            {
                return new Team();
            }

            return await GetTeamFromReplay(link, user, playerValue, jsonLink, replayObject)
                .ConfigureAwait(false);
        }

        private static string GetJsonLink(Uri link)
        {
            var jsonLink = link.ToString();
            // Remove additions like "?p2"
            if (!string.IsNullOrWhiteSpace(link.Query))
            {
                jsonLink = jsonLink.Replace(link.Query, "");
            }
            if (!jsonLink.Contains(".json"))
            {
                jsonLink += ".json";
            }
            return jsonLink;
        }

        private static async Task<Replay?> GetReplayFromUrl(string jsonLink)
        {
            var replayResult = await Common.HttpClient.GetAsync(jsonLink).ConfigureAwait(false);
            if (!replayResult.IsSuccessStatusCode)
            {
                // No success might mean 404 or the server is down, anyway we shut this down gracefully
                return null;
            }

            var replayJson = await replayResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            // Old error handling, sometimes "<" is returned from the API
            if (replayJson.StartsWith("<"))
            {
                // Use get String as the assumption is the first request went through, this will so as well
                replayJson = await Common.HttpClient.GetStringAsync(jsonLink).ConfigureAwait(false);
                // If return twice, at least don't break everything
                if (replayJson.StartsWith("<"))
                {
                    return null;
                }
            }
            if (replayJson == "Could not connect")
            {
                return null;
            }
            return JsonConvert.DeserializeObject<Replay>(replayJson);
        }

        private async Task<Team> GetTeamFromReplay(
            Uri link,
            string? user,
            string playerValue,
            string jsonLink,
            Replay sourceReplayObject
        )
        {
            var playerInfo = new PlayerInfo { PlayerName = "", PlayerValue = playerValue };

            var team = new Team();
            var replayObject = sourceReplayObject.Clone();
            team.Replays.Add(replayObject);
            replayObject.Link = link;
            var replayLog = replayObject.Log;
            team.Format = replayObject.Format;
            if (playerValue == "p1")
            {
                if (replayObject.P1 is not null)
                {
                    playerInfo.PlayerName = replayObject.P1;
                }
                else
                {
                    playerInfo.PlayerName = replayObject.Players.FirstOrDefault();
                    replayObject.P1 = replayObject.Players.FirstOrDefault();
                }
            }
            if (playerValue == "p2")
            {
                if (replayObject.P2 is not null)
                {
                    playerInfo.PlayerName = replayObject.P2;
                }
                else
                {
                    playerInfo.PlayerName = replayObject.Players.LastOrDefault();
                    replayObject.P2 = replayObject.Players.LastOrDefault();
                }
            }
            replayObject.PlayerInfo = playerInfo;

            try
            {
                ReplayLogAnalyzer.Analyze(
                    user,
                    playerInfo,
                    team,
                    replayLog,
                    replayObject,
                    new MoveRules(IllegalMoves, ItemTransformingMoves, ZStatusMoveItems)
                );
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(
                    $"Error on replay {jsonLink}, canceling analyzing after exception:"
                );
                Console.Error.WriteLine(e);
                Console.Error.WriteLine(e.StackTrace);
            }

            if (team.Pokemon.Count > 0)
            {
                await SetCache(playerInfo, jsonLink, team).ConfigureAwait(false);
            }

            return team;
        }

        /// <summary>
        /// Increase whenever the analysis changes, so teams analyzed by an older version are not reused.
        /// </summary>
        public const int AnalysisVersion = 2;

        private static string TeamCacheKey(string logLink, string? player)
        {
            return $"{logLink}+v{AnalysisVersion}+{player}";
        }

        private async Task<Team?> GetFromCache(string? user, string playerValue, string logLink)
        {
            Team? cachedTeam = null;
            if (_cache != null)
            {
                string? result = null;
                try
                {
                    result = await _cache
                        .GetStringAsync(TeamCacheKey(logLink, user))
                        .ConfigureAwait(false);
                    result ??= await _cache
                        .GetStringAsync(TeamCacheKey(logLink, playerValue))
                        .ConfigureAwait(false);
                }
                catch (NullReferenceException) { }
                if (result != null)
                {
                    cachedTeam = JsonConvert.DeserializeObject<Team>(result);
                }
            }

            return cachedTeam;
        }

        private readonly object replayLock = new();

        private async Task SetCache(PlayerInfo playerInfo, string logLink, Team team)
        {
            if (_cache != null)
            {
                var serializedTeam = JsonConvert.SerializeObject(team);
                await _cache
                    .SetStringAsync(TeamCacheKey(logLink, playerInfo.PlayerName), serializedTeam)
                    .ConfigureAwait(false);
                await _cache
                    .SetStringAsync(TeamCacheKey(logLink, playerInfo.PlayerValue), serializedTeam)
                    .ConfigureAwait(false);
                // Lock since otherwise this can become a race condition
                lock (replayLock)
                {
                    var currentCachedLinkList = new List<CachedLink>();
                    string? currentReplays = null;
                    try
                    {
                        currentReplays = _cache.GetString($"replays-{playerInfo.PlayerName}");
                    }
                    catch (NullReferenceException) { }
                    var uriLogLink = new Uri(logLink);
                    if (currentReplays != null)
                    {
                        currentCachedLinkList =
                            JsonConvert.DeserializeObject<List<CachedLink>>(currentReplays)
                            ?? currentCachedLinkList;
                    }
                    if (
                        !currentCachedLinkList.Any(
                            (cachedLink) => cachedLink.ReplayLog == uriLogLink
                        )
                    )
                    {
                        currentCachedLinkList.Add(
                            new CachedLink() { ReplayLog = uriLogLink, Format = team.Format }
                        );
                    }
                    _cache.SetString(
                        $"replays-{playerInfo.PlayerName}",
                        JsonConvert.SerializeObject(currentCachedLinkList)
                    );
                }
            }
        }

        /// <summary>
        /// Injectable List of Moves which are not added (e.g. "Struggle")
        /// </summary>
        public IEnumerable<string> IllegalMoves = ["Struggle"];

        /// <summary>
        /// Moves that trigger a specific item so be set (e.g. Z-Moves)
        /// The specific moves are not accounted.
        /// </summary>
        public IDictionary<string, string> ItemTransformingMoves = new Dictionary<string, string>
        {
            { "Breakneck Blitz", "Normalium Z" },
            { "All-Out Pummeling", "Fightinium Z" },
            { "Supersonic Skystrike", "Flyinium Z" },
            { "Acid Downpour", "Poisonium Z" },
            { "Tectonic Rage", "Groundium Z" },
            { "Continental Crush", "Rockium Z" },
            { "Savage Spin-Out", "Buginium Z" },
            { "Never-Ending Nightmare", "Ghostium Z" },
            { "Corkscrew Crash", "Steelium Z" },
            { "Inferno Overdrive", "Firium Z" },
            { "Hydro Vortex", "Waterium Z" },
            { "Bloom Doom", "Grassium Z" },
            { "Gigavolt Havoc", "Electrium Z" },
            { "Shattered Psyche", "Psychium Z" },
            { "Subzero Slammer", "Icium Z" },
            { "Devastating Drake", "Dragonium Z" },
            { "Black Hole Eclipse", "Darkinium Z" },
            { "Twinkle Tackle", "Fairium Z" },
            { "Catastropika", "Pikanium Z" },
            { "Sinister Arrow Raid", "Decidium Z" },
            { "Malicious Moonsault", "Incinium Z" },
            { "Oceanic Operetta", "Primarium Z" },
            { "Guardian of Alola", "Tapunium Z" },
            { "Soul-Stealing 7-Star Strike", "Marshadium Z" },
            { "Stoked Sparksurfer", "Aloraichium Z" },
            { "Pulverizing Pancake", "Snorlium Z" },
            { "Extreme Evoboost", "Eevium Z" },
            { "Genesis Supernova", "Mewnium Z" },
            { "10,000,000 Volt Thunderbolt", "Pikashunium Z" },
            { "Clangorous Soulblaze", "Kommonium Z" },
            { "Splintered Stormshards", "Lycanium Z" },
            { "Searing Sunraze Smash", "Solganium Z" },
            { "Menacing Moonraze Maelstrom", "Lunalium Z" },
            { "Light That Burns the Sky", "Ultranecrozium Z" },
            { "Let's Snuggle Forever", "Mimikium Z" },
        };

        /// <summary>
        /// Status moves whose Z-powered variant ("Z-Move") reveals a specific Z-Crystal.
        /// </summary>
        public IDictionary<string, string> ZStatusMoveItems = new Dictionary<string, string>(
            ZStatusMoves.Crystals
        );
    }
}
