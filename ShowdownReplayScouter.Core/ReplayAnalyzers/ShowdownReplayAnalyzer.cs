using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers
{
    public class ShowdownReplayAnalyzer : IReplayAnalyzer
    {
        public ShowdownReplayAnalyzer() : this(null) { }

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
            return new List<Team> {
                await GetTeamFromUrl(replay, playerValue: "p1").ConfigureAwait(false),
                await GetTeamFromUrl(replay, playerValue: "p2").ConfigureAwait(false),
            };
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
            return new List<Team> {
                await GetTeamFromUrl(replay, user).ConfigureAwait(false)
            };
        }

        private async Task<Team> GetTeamFromUrl(Uri link, string? user = null, string playerValue = "")
        {
            var jsonLink = link.ToString();
            if (!jsonLink.Contains(".json"))
            {
                jsonLink += ".json";
            }

            var cachedTeam = await GetFromCache(user, playerValue, jsonLink).ConfigureAwait(false);
            if (cachedTeam != null)
            {
                return cachedTeam;
            }

            var playerInfo = new PlayerInfo
            {
                PlayerName = "",
                PlayerValue = playerValue
            };

            var team = new Team();

            var replayJson = await Common.HttpClient.GetStringAsync(jsonLink).ConfigureAwait(false);
            if (replayJson == "Could not connect")
            {
                return team;
            }
            var replayLog = "";
            Replay? replayObject = null;
            if (replayJson is not null && replayJson != "Could not connect")
            {
                replayObject = JsonConvert.DeserializeObject<Replay>(replayJson);
                if (replayObject is not null)
                {
                    team.Replays.Add(replayObject);
                    replayObject.Link = link;
                    replayLog = replayObject.Log;
                    team.Format = replayObject.Format;
                    if (playerValue == "p1")
                    {
                        playerInfo.PlayerName = replayObject.P1;
                    }
                    if (playerValue == "p2")
                    {
                        playerInfo.PlayerName = replayObject.P2;
                    }
                    replayObject.PlayerInfo = playerInfo;
                }
            }

            foreach (var line in replayLog.Split('\n'))
            {
                if (line.Contains("|player"))
                {
                    DeterminePlayer(user, playerInfo, line);
                }
                else if (playerInfo.PlayerValue?.Length == 0)
                {
                    continue;
                }
                else if (line.Contains("|poke|"))
                {
                    var pokeinf = line.Split('|');
                    if (pokeinf[2] == playerInfo.PlayerValue)
                    {
                        team.Pokemon.Add(new Pokemon()
                        {
                            Name = pokeinf[3].Split(',')[0],
                            AltNames = { pokeinf[3].Split('-')[0] }
                        });
                    }
                }
                else if (line.Contains("|switch") || line.Contains("|drag"))
                {
                    if (line.Contains(playerInfo.PlayerValue!))
                    {
                        var pokeinf = line.Split('|');
                        var maybepoke = pokeinf[3].Split(',')[0];
                        var pokemon = team.Pokemon.FirstOrDefault((pokemon) => pokemon.Name == maybepoke || pokemon.FormName == maybepoke);
                        if (pokemon == null)
                        {
                            pokemon = team.Pokemon.FirstOrDefault((pokemon) =>
                            {
                                return Common.FormPokemonList
                                    .Any((formPokemon) => formPokemon == pokemon.Name && maybepoke.Contains(formPokemon));
                            });
                            if (pokemon != null)
                            {
                                pokemon.FormName = maybepoke;
                            }
                            else
                            {
                                pokemon = new Pokemon()
                                {
                                    Name = maybepoke
                                };
                                if (team.Pokemon.Count == 0)
                                {
                                    pokemon.Lead = true;
                                }
                                team.Pokemon.Add(pokemon);
                            }
                        }
                        if (pokeinf[2].Contains(':'))
                        {
                            var nick = pokeinf[2].Split(':')[1].Trim();
                            if (!pokemon.AltNames.Any((altName) => altName == nick))
                            {
                                pokemon.AltNames.Add(nick);
                            }
                        }
                    }
                }
                else if (line.Contains("|move|") && !line.Contains("|-sidestart"))
                {
                    var moveinf = line.Split('|');
                    if (moveinf[2].Contains(':'))
                    {
                        var playerString = moveinf[2][..moveinf[2].IndexOf(":")];
                        if (playerString.Contains(playerInfo.PlayerValue!))
                        {
                            var mon = moveinf[2].Split(':')[1].Trim();
                            var move = moveinf[3];
                            var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                            if (line.Contains("[from]Magic Bounce"))
                            {
                                AbilityUpdate(pokemon, "Magic Bounce");
                            }
                            else
                            {
                                MoveUpdate(pokemon, move);
                            }
                        }
                    }
                }
                else if (line.Contains("|detailschange"))
                {
                    var detailinf = line.Split('|');
                    if (detailinf[2].Contains(playerInfo.PlayerValue!))
                    {
                        var mon = detailinf[2].Split(':')[1].Trim();
                        var newmon = detailinf[3].Split(',')[0];
                        var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                        if (pokemon.FormName != newmon)
                        {
                            pokemon.FormName = newmon;
                        }
                    }
                }
                else if (line.Contains("[from] item:") && !(line.Contains("-damage") && line.Contains("Rocky Helmet")))
                {
                    var iteminf = line.Split('|');
                    if (iteminf[2].Contains(':'))
                    {
                        var playerString = iteminf[2][..iteminf[2].IndexOf(":")];

                        if (playerString.Contains(playerInfo.PlayerValue!))
                        {
                            var mon = iteminf[2].Split(':')[1].Trim();
                            var item = "";
                            if (iteminf[4].Contains("item"))
                            {
                                item = iteminf[4].Split(':')[1].Trim();
                            }
                            else
                            {
                                item = iteminf[5].Split(':')[1].Trim();
                            }
                            var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                            ItemUpdate(pokemon, item);
                        }
                    }
                }
                else if (line.Contains("-damage") && line.Contains("Rocky Helmet"))
                {
                    var iteminf = line.Split('|');
                    if (iteminf.Length > 5 && iteminf[5].Contains(playerInfo.PlayerValue!))
                    {
                        var mon = iteminf[5].Split(':')[1].Trim();
                        var item = iteminf[4].Split(':')[1].Trim();
                        var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                        ItemUpdate(pokemon, item);
                    }
                }
                else if (line.Contains("-enditem"))
                {
                    var iteminf = line.Split('|');
                    if (iteminf[2].Contains(':'))
                    {
                        var playerString = iteminf[2][..iteminf[2].IndexOf(":")];

                        if (playerString.Contains(playerInfo.PlayerValue!))
                        {
                            var mon = iteminf[2].Split(':')[1].Trim();
                            var item = iteminf[3].Trim();
                            var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                            ItemUpdate(pokemon, item);
                        }
                    }
                }
                else if ((line.Contains("-ability|") || line.Contains(" ability:")) && line.Contains("-damage"))
                {
                    var abilityinf = line.Split('|');
                    var ability = "";
                    var mon = "";
                    ability = abilityinf[4].Split(':')[1].Trim();
                    if (abilityinf.Length > 5)
                    {
                        mon = abilityinf[5].Split(':')[1].Trim();
                        if (abilityinf[5].Split(':')[1].Contains(playerInfo.PlayerValue!))
                        {
                            var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                            AbilityUpdate(pokemon, ability);
                        }
                    }
                }
                else if (line.Contains("ability:") && !line.Contains("ability: Imposter"))
                {
                    var abilityinf = line.Split('|');
                    var ability = "";
                    var mon = "";
                    foreach (var abinf in abilityinf)
                    {
                        if (abinf.Contains("ability:"))
                        {
                            ability = abinf.Split(':')[1].Trim();
                        }
                        else if (abinf.Contains(':') && abinf.Split(':')[0].Contains(playerInfo.PlayerValue!))
                        {
                            if (!abinf.Contains("[of]") && !ContainsLineAnOfAbility(line))
                            {
                                mon = abinf.Split(':')[1].Trim();
                            }
                            else if (abinf.Contains("[of]") && ContainsLineAnOfAbility(line))
                            {
                                mon = abinf.Split(':')[1].Trim();
                            }
                        }
                    }
                    if (ability != "" && mon != "")
                    {
                        var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                        AbilityUpdate(pokemon, ability);
                    }
                }
                else if (line.Contains("-ability"))
                {
                    var abilityinf = line.Split('|');
                    if (abilityinf[2].Contains(':') && abilityinf[2].Split(':')[0].Contains(playerInfo.PlayerValue!))
                    {
                        var ability = abilityinf[3].Trim();
                        var mon = abilityinf[2].Split(':')[1].Trim();

                        var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                        AbilityUpdate(pokemon, ability);
                    }
                }
                else if (line.Contains("-terastallize"))
                {
                    var terainf = line.Split('|');
                    if (terainf[2].Contains(':') && terainf[2].Split(':')[0].Contains(playerInfo.PlayerValue!))
                    {
                        var teraType = terainf[3].Trim();
                        var mon = terainf[2].Split(':')[1].Trim();

                        var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                        TeraUpdate(pokemon, teraType);
                    }
                }
                else if (line.Contains("|win|"))
                {
                    var terainf = line.Split('|');
                    if (replayObject is not null && terainf.Length > 2)
                    {
                        replayObject.Winner = terainf[2];
                        replayObject.WinForTeam =
                            (replayObject.Winner == replayObject.P1 && playerInfo.PlayerValue == "p1")
                            || (replayObject.Winner == replayObject.P2 && playerInfo.PlayerValue == "p2");
                    }
                }
            }

            if (team.Pokemon.Count > 0)
            {
                await SetCache(playerInfo, jsonLink, team).ConfigureAwait(false);
            }

            return team;
        }

        private async Task<Team?> GetFromCache(string? user, string playerValue, string logLink)
        {
            Team? cachedTeam = null;
            if (_cache != null)
            {
                string? result = null;
                try
                {
                    result = await _cache.GetStringAsync($"{logLink}+{user}").ConfigureAwait(false);
                    result ??= await _cache.GetStringAsync($"{logLink}+{playerValue}").ConfigureAwait(false);
                }
                catch (NullReferenceException)
                {
                }
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
                await _cache.SetStringAsync($"{logLink}+{playerInfo.PlayerName}", serializedTeam).ConfigureAwait(false);
                await _cache.SetStringAsync($"{logLink}+{playerInfo.PlayerValue}", serializedTeam).ConfigureAwait(false);
                // Lock since otherwise this can become a race condition
                lock (replayLock)
                {
                    var currentCachedLinkList = new List<CachedLink>();
                    string? currentReplays = null;
                    try
                    {
                        currentReplays = _cache.GetString($"replays-{playerInfo.PlayerName}");
                    }
                    catch (NullReferenceException)
                    {
                    }
                    var uriLogLink = new Uri(logLink);
                    if (currentReplays != null)
                    {
                        currentCachedLinkList =
                            JsonConvert.DeserializeObject<List<CachedLink>>(currentReplays)
                            ?? currentCachedLinkList;
                    }
                    if (!currentCachedLinkList.Any((cachedLink) => cachedLink.ReplayLog == uriLogLink))
                    {
                        currentCachedLinkList.Add(new CachedLink()
                        {
                            ReplayLog = uriLogLink,
                            Format = team.Format
                        });
                    }
                    _cache.SetString($"replays-{playerInfo.PlayerName}", JsonConvert.SerializeObject(currentCachedLinkList));
                }
            }
        }

        private static void DeterminePlayer(string? rawUser, PlayerInfo playerInfo, string line)
        {
            var setPlayer = false;
            var playerinf = line.Split('|');
            var regexedPlayerInf = "";
            if (playerinf.Length > 3)
            {
                regexedPlayerInf = RegexUtil.Regex(playerinf[3].ToLower());
            }

            if (rawUser != null)
            {
                var user = RegexUtil.Regex(rawUser.ToLower());
                if (playerinf.Length > 3)
                {
                    var distance = LevenshteinDistance.Compute(regexedPlayerInf, user);
                    if (regexedPlayerInf.Contains(user))
                    {
                        if (!string.IsNullOrWhiteSpace(playerInfo.PlayerName))
                        {
                            if (playerInfo.PlayerName.Contains(user))
                            {
                                if (LevenshteinDistance.Compute(playerInfo.PlayerName, user) > distance)
                                {
                                    setPlayer = true;
                                }
                            }
                            else
                            {
                                setPlayer = true;
                            }
                        }
                        else
                        {
                            setPlayer = true;
                        }
                    }
                    else if (distance <= Common.LevenshteinDistanceAcceptable)
                    {
                        if (!string.IsNullOrWhiteSpace(playerInfo.PlayerName))
                        {
                            if (LevenshteinDistance.Compute(playerInfo.PlayerName, user) > distance && !playerInfo.PlayerName.Contains(user))
                            {
                                setPlayer = true;
                            }
                        }
                        else
                        {
                            setPlayer = true;
                        }
                    }
                }
            }

            if (setPlayer)
            {
                playerInfo.PlayerValue = playerinf[2];
                playerInfo.PlayerName = regexedPlayerInf;
            }

            if (playerInfo.PlayerValue == playerinf[2] && playerInfo.PlayerName?.Length == 0)
            {
                playerInfo.PlayerName = regexedPlayerInf;
            }
        }

        private static bool ContainsLineAnOfAbility(string line)
        {
            foreach (var ofAbility in Common.OfAbilities)
            {
                if (line.Contains($"ability: {ofAbility}"))
                {
                    return true;
                }
            }
            return false;
        }

        private static Pokemon AddMonIfNotExists(ICollection<Pokemon> pokemonList, string pokemonCandidate)
        {
            var pokemon = pokemonList.FirstOrDefault((pokemon) => pokemon.Name == pokemonCandidate
                || pokemon.FormName == pokemonCandidate
                || pokemon.AltNames.Any((altName) => altName == pokemonCandidate));
            if (pokemon == null)
            {
                var regexPokemonCandidate = RegexUtil.Regex(pokemonCandidate);
                pokemon = pokemonList.FirstOrDefault((pokemon) =>
                {
                    return Common.FormDescriptorList
                        .Select((formDescriptor) => RegexUtil.Regex($"{pokemon.Name}-{formDescriptor}"))
                        .Any((possibleForm) => possibleForm == regexPokemonCandidate);
                });
                if (pokemon != null)
                {
                    pokemon.FormName = pokemonCandidate;
                }
                pokemon = pokemonList.FirstOrDefault((pokemon) =>
                {
                    return Common.FormPokemonList
                        .Any((formPokemon) => formPokemon == pokemon.Name && pokemonCandidate.Contains(formPokemon));
                });
                if (pokemon != null)
                {
                    pokemon.FormName = pokemonCandidate;
                }
                else
                {
                    pokemon = new Pokemon()
                    {
                        Name = pokemonCandidate
                    };
                    pokemonList.Add(pokemon);
                }
            }
            return pokemon;
        }

        private static void AbilityUpdate(Pokemon pokemon, string ability)
        {
            if (pokemon.Ability != null)
            {
                if (!pokemon.Ability.Contains(ability))
                {
                    pokemon.Ability += " | " + ability;
                }
            }
            else
            {
                pokemon.Ability = ability;
            }
        }

        private static void TeraUpdate(Pokemon pokemon, string teraType)
        {
            if (pokemon.TeraType != null)
            {
                if (!pokemon.TeraType.Contains(teraType))
                {
                    pokemon.TeraType += " | " + teraType;
                }
            }
            else
            {
                pokemon.TeraType = teraType;
            }
        }

        private static void ItemUpdate(Pokemon pokemon, string item)
        {
            if (pokemon.Item == null || pokemon.Item?.Length == 0)
            {
                pokemon.Item = item;
            }
            else
            {
                if (pokemon.Item?.Contains(item) == false)
                {
                    pokemon.Item += " | " + item;
                }
            }
        }

        /// <summary>
        /// Injectable List of Moves which are not added (e.g. "Struggle")
        /// </summary>
        public IEnumerable<string> IllegalMoves = new List<string>
        {
            "Struggle"
        };

        /// <summary>
        /// Moves that trigger a specific item so be set (e.g. Z-Moves)
        /// The specific moves are not accounted.
        /// </summary>
        public IDictionary<string, string> ItemTransformingMoves = new Dictionary<string, string>
        {
            {"Breakneck Blitz", "Normalium Z"},
            {"All-Out Pummeling", "Fightinium Z"},
            {"Supersonic Skystrike", "Flyinium Z"},
            {"Acid Downpour", "Poisonium Z"},
            {"Tectonic Rage", "Groundium Z"},
            {"Continental Crush", "Rockium Z"},
            {"Savage Spin-Out", "Buginium Z"},
            {"Never-Ending Nightmare", "Ghostium Z"},
            {"Corkscrew Crash", "Steelium Z"},
            {"Inferno Overdrive", "Firium Z"},
            {"Hydro Vortex", "Waterium Z"},
            {"Bloom Doom", "Grassium Z"},
            {"Gigavolt Havoc", "Electrium Z"},
            {"Shattered Psyche", "Psychium Z"},
            {"Subzero Slammer", "Icium Z"},
            {"Devastating Drake", "Dragonium Z"},
            {"Black Hole Eclipse", "Darkinium Z"},
            {"Twinkle Tackle", "Fairium Z" },
            {"Catastropika", "Pikanium Z"},
            {"Sinister Arrow Raid", "Decidium Z"},
            {"Malicious Moonsault", "Incinium Z"},
            {"Oceanic Operetta", "Primarium Z"},
            {"Guardian of Alola", "Tapunium Z"},
            {"Soul-Stealing 7-Star Strike", "Marshadium Z"},
            {"Stoked Sparksurfer", "Aloraichium Z"},
            {"Pulverizing Pancake", "Snorlium Z"},
            {"Extreme Evoboost", "Eevium Z"},
            {"Genesis Supernova", "Mewnium Z"},
            {"10,000,000 Volt Thunderbolt", "Pikashunium Z"},
            {"Clangorous Soulblaze", "Kommonium Z"},
            {"Splintered Stormshards", "Lycanium Z"},
            {"Searing Sunraze Smash", "Solganium Z"},
            {"Menacing Moonraze Maelstrom", "Lunalium Z"},
            {"Light That Burns the Sky", "Ultranecrozium Z"},
            {"Let's Snuggle Forever", "Mimikium Z"},
        };

        private void MoveUpdate(Pokemon pokemon, string move)
        {
            if (ItemTransformingMoves.ContainsKey(move))
            {
                ItemUpdate(pokemon, ItemTransformingMoves[move]);
            }
            else if (!pokemon.Moves.Contains(move) && !IllegalMoves.Contains(move))
            {
                pokemon.Moves.Add(move);
            }
        }
    }
}
