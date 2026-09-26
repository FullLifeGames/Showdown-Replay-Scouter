using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using ShowdownReplayScouter.Core.Data;
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
                AnalyzeLogic(user, playerInfo, team, replayLog, replayObject);
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
        /// Per replay state that is needed to attribute protocol messages correctly.
        /// </summary>
        private sealed class BattleState
        {
            /// <summary>
            /// Maps the nickname used in protocol idents ("p1a: Nickname") to the Pokemon.
            /// </summary>
            public Dictionary<string, Pokemon> Nicknames { get; } = [];

            /// <summary>
            /// Pokemon currently holding an item they did not bring (e.g. after Trick).
            /// </summary>
            public HashSet<Pokemon> ForeignItemHolders { get; } = [];

            /// <summary>
            /// Pokemon whose set was posted via "!showteam" / "!showset".
            /// </summary>
            public HashSet<Pokemon> PostedSets { get; } = [];

            public string? TrickUser { get; set; }
            public string? TrickTarget { get; set; }
            public HashSet<Pokemon> ForeignItemHoldersBeforeTrick { get; set; } = [];
        }

        private sealed class LineContext(
            Team team,
            PlayerInfo playerInfo,
            BattleState state,
            string[] parts
        )
        {
            public Team Team { get; } = team;
            public PlayerInfo PlayerInfo { get; } = playerInfo;
            public BattleState State { get; } = state;
            public string[] Parts { get; } = parts;
            public string Command => Parts[1];

            public string? Arg(int index)
            {
                return Parts.Length > index ? Parts[index] : null;
            }

            /// <summary>
            /// The main Pokemon of the message (for e.g. "|-status|p1a: Nick|..." this is "p1a: Nick").
            /// </summary>
            public string? Main => Command.StartsWith("-side") ? null : Arg(2);

            public string? From => GetKwarg("[from]");
            public string? Of => GetKwarg("[of]");

            private string? GetKwarg(string key)
            {
                var part = Parts.Skip(2).FirstOrDefault((part) => part.StartsWith(key));
                return part?[key.Length..].Trim();
            }

            /// <summary>
            /// Returns the Pokemon of the scouted player for an ident like "p1a: Nick", null otherwise.
            /// </summary>
            public Pokemon? Resolve(string? ident)
            {
                if (!TryParseIdent(ident, out var side, out var nickname))
                {
                    return null;
                }
                if (!side.StartsWith(PlayerInfo.PlayerValue!))
                {
                    return null;
                }
                if (State.Nicknames.TryGetValue(nickname, out var pokemon))
                {
                    return pokemon;
                }
                return AddMonIfNotExists(Team.Pokemon, nickname);
            }
        }

        private static bool TryParseIdent(string? ident, out string side, out string nickname)
        {
            side = "";
            nickname = "";
            if (ident is null)
            {
                return false;
            }
            var separator = ident.IndexOf(':');
            if (separator <= 0)
            {
                return false;
            }
            side = ident[..separator].Trim();
            nickname = ident[(separator + 1)..].Trim();
            return side.Length is 2 or 3 && side[0] == 'p' && char.IsDigit(side[1]);
        }

        private static bool SameIdent(string? first, string? second)
        {
            return TryParseIdent(first, out var firstSide, out var firstNickname)
                && TryParseIdent(second, out var secondSide, out var secondNickname)
                && firstSide == secondSide
                && firstNickname == secondNickname;
        }

        private void AnalyzeLogic(
            string? user,
            PlayerInfo playerInfo,
            Team team,
            string replayLog,
            Replay? replayObject
        )
        {
            var state = new BattleState();
            foreach (var line in replayLog.Split('\n'))
            {
                var parts = line.TrimEnd('\r').Split('|');
                if (parts.Length < 2)
                {
                    continue;
                }
                if (parts[1] == "player")
                {
                    DeterminePlayer(user, playerInfo, line);
                    continue;
                }
                if (playerInfo.PlayerValue?.Length == 0)
                {
                    continue;
                }

                var context = new LineContext(team, playerInfo, state, parts);
                switch (context.Command)
                {
                    case "poke":
                        HandlePoke(context);
                        break;
                    case "switch":
                    case "drag":
                    case "replace":
                        HandleSwitch(context);
                        break;
                    case "move":
                        HandleMove(context);
                        break;
                    case "detailschange":
                        HandleDetailsChange(context);
                        break;
                    case "-terastallize":
                        HandleTerastallize(context);
                        break;
                    case "c":
                        HandleChat(context);
                        break;
                    case "win":
                        HandleWin(context, replayObject);
                        break;
                    default:
                        HandleItems(context);
                        HandleAbilities(context);
                        break;
                }
            }
        }

        private static void HandlePoke(LineContext context)
        {
            var pokeinf = context.Parts;
            if (pokeinf.Length > 3 && pokeinf[2] == context.PlayerInfo.PlayerValue)
            {
                context.Team.Pokemon.Add(
                    new Pokemon()
                    {
                        Name = pokeinf[3].Split(',')[0],
                        AltNames = { pokeinf[3].Split('-')[0] }
                    }
                );
            }
        }

        private static void HandleSwitch(LineContext context)
        {
            var pokeinf = context.Parts;
            if (pokeinf.Length < 4)
            {
                // Something broke in the replay, skipping
                return;
            }
            if (
                !TryParseIdent(pokeinf[2], out var side, out var nickname)
                || !side.StartsWith(context.PlayerInfo.PlayerValue!)
            )
            {
                return;
            }

            var team = context.Team;
            var nicknames = context.State.Nicknames;
            var maybepoke = pokeinf[3].Split(',')[0];
            if (nicknames.TryGetValue(nickname, out var pokemon))
            {
                if (pokemon.Name != maybepoke && pokemon.FormName != maybepoke)
                {
                    pokemon.FormName = maybepoke;
                }
                return;
            }

            // Pokemon already identified by another nickname are a different Pokemon,
            // this keeps e.g. two Heracross in the same team apart
            var unclaimed = team
                .Pokemon.Where((pokemon) => !nicknames.ContainsValue(pokemon))
                .ToList();
            pokemon = unclaimed.FirstOrDefault(
                (pokemon) => pokemon.Name == maybepoke || pokemon.FormName == maybepoke
            );
            if (pokemon == null)
            {
                pokemon = unclaimed.FirstOrDefault(
                    (pokemon) =>
                        Common.FormPokemonList.Any(
                            (formPokemon) =>
                                formPokemon == pokemon.Name && maybepoke.Contains(formPokemon)
                        )
                );
                pokemon ??= unclaimed.FirstOrDefault(
                    (pokemon) =>
                        pokemon.Name == nickname
                        || pokemon.FormName == nickname
                        || pokemon.AltNames.Any((altName) => altName == nickname)
                );
                if (pokemon != null)
                {
                    pokemon.FormName = maybepoke;
                }
                else
                {
                    pokemon = new Pokemon() { Name = maybepoke };
                    if (team.Pokemon.Count == 0)
                    {
                        pokemon.Lead = true;
                    }
                    team.Pokemon.Add(pokemon);
                }
            }

            nicknames[nickname] = pokemon;
            if (!pokemon.AltNames.Any((altName) => altName == nickname))
            {
                pokemon.AltNames.Add(nickname);
            }
        }

        private void HandleMove(LineContext context)
        {
            var move = context.Arg(3);
            if (move is null)
            {
                return;
            }
            if (move is "Trick" or "Switcheroo")
            {
                StartItemSwap(context, context.Arg(2), context.Arg(4));
            }

            var pokemon = context.Resolve(context.Arg(2));
            if (pokemon is null)
            {
                return;
            }

            var from = context.From;
            if (from is not null)
            {
                if (from.StartsWith("ability:"))
                {
                    // Moves used by an ability (e.g. Magic Bounce, Dancer) are not part of the set
                    AbilityUpdate(pokemon, from["ability:".Length..].Trim());
                    return;
                }
                if (from.Contains("Magic Bounce"))
                {
                    AbilityUpdate(pokemon, "Magic Bounce");
                    return;
                }
                if (from == "move: Metronome")
                {
                    // Only Metronome itself is part of the set, not the move it called
                    return;
                }
            }

            MoveUpdate(pokemon, move);
        }

        private static void HandleDetailsChange(LineContext context)
        {
            var newmon = context.Arg(3)?.Split(',')[0];
            var pokemon = context.Resolve(context.Arg(2));
            if (pokemon is not null && newmon is not null && pokemon.FormName != newmon)
            {
                pokemon.FormName = newmon;
            }
        }

        private static void HandleTerastallize(LineContext context)
        {
            var teraType = context.Arg(3)?.Trim();
            var pokemon = context.Resolve(context.Arg(2));
            if (pokemon is not null && !string.IsNullOrEmpty(teraType))
            {
                TeraUpdate(pokemon, teraType);
            }
        }

        private static void HandleWin(LineContext context, Replay? replayObject)
        {
            var winner = context.Arg(2);
            if (replayObject is not null && winner is not null)
            {
                replayObject.Winner = winner;
                var regexWinner = RegexUtil.Regex(replayObject.Winner).ToLower();
                replayObject.WinForTeam = regexWinner.Equals(
                    RegexUtil.Regex(context.PlayerInfo.PlayerName),
                    StringComparison.CurrentCultureIgnoreCase
                );
            }
        }

        /// <summary>
        /// Sets posted with "!showteam" / "!showset" by the scouted player are the actual sets.
        /// </summary>
        private static void HandleChat(LineContext context)
        {
            var sender = context.Arg(2);
            if (sender is null || context.Parts.Length < 4)
            {
                return;
            }
            var message = string.Join("|", context.Parts.Skip(3));
            if (!ShowdownSetParser.IsPostedSets(message))
            {
                return;
            }
            var regexSender = RegexUtil.Regex(sender);
            if (
                regexSender.Length == 0
                || regexSender != RegexUtil.Regex(context.PlayerInfo.PlayerName)
            )
            {
                return;
            }

            foreach (var set in ShowdownSetParser.ParseRawHtml(message))
            {
                ApplyPostedSet(context, set);
            }
        }

        private static void ApplyPostedSet(LineContext context, ShowdownSet set)
        {
            var state = context.State;
            var team = context.Team;
            Pokemon? pokemon = null;
            if (set.Nickname is not null)
            {
                state.Nicknames.TryGetValue(set.Nickname, out pokemon);
            }
            var candidates = team
                .Pokemon.Where((pokemon) => !state.PostedSets.Contains(pokemon))
                .ToList();
            pokemon ??= candidates.FirstOrDefault(
                (pokemon) =>
                    set.Nickname is not null
                    && pokemon.AltNames.Any((altName) => altName == set.Nickname)
            );
            pokemon ??= candidates.FirstOrDefault(
                (pokemon) =>
                    (pokemon.Name == set.Species || pokemon.FormName == set.Species)
                    && !state.Nicknames.ContainsValue(pokemon)
            );
            pokemon ??= candidates.FirstOrDefault(
                (pokemon) => pokemon.Name == set.Species || pokemon.FormName == set.Species
            );
            if (pokemon is null)
            {
                pokemon = new Pokemon() { Name = set.Species };
                team.Pokemon.Add(pokemon);
            }
            if (
                set.Nickname is not null
                && !pokemon.AltNames.Any((altName) => altName == set.Nickname)
            )
            {
                pokemon.AltNames.Add(set.Nickname);
            }

            state.PostedSets.Add(pokemon);
            if (set.Item is not null)
            {
                pokemon.Item = set.Item;
            }
            if (set.Ability is not null)
            {
                pokemon.Ability = set.Ability;
            }
            if (set.TeraType is not null)
            {
                pokemon.TeraType = set.TeraType;
            }
            if (set.Moves.Count > 0)
            {
                pokemon.Moves = set.Moves.ToList();
            }
        }

        /// <summary>
        /// Items that hurt the attacker, so the damaged Pokemon is not the holder.
        /// </summary>
        private static readonly HashSet<string> AttackerDamagingItems =
        [
            "Rocky Helmet",
            "Jaboca Berry",
            "Rowap Berry"
        ];

        /// <summary>
        /// Effects that give a Pokemon an item it did not bring, the [of] Pokemon (if any) brought it.
        /// </summary>
        private static readonly HashSet<string> ItemTransferEffects =
        [
            "move: Thief",
            "move: Covet",
            "move: Bestow",
            "ability: Magician",
            "ability: Pickpocket",
            "ability: Pickup"
        ];

        private static void HandleItems(LineContext context)
        {
            var from = context.From;
            switch (context.Command)
            {
                case "-item":
                    HandleItemGain(context);
                    break;
                case "-enditem":
                    ItemReveal(context, context.Main, context.Arg(3));
                    break;
                case "-activate":
                    var effect = context.Arg(3)?.Trim();
                    if (effect is "move: Trick" or "move: Switcheroo")
                    {
                        StartItemSwap(context, context.Main, context.Of);
                    }
                    else if (effect == "move: Poltergeist")
                    {
                        // "|-activate|p1a: Target|move: Poltergeist|Item" reveals the item of the target
                        ItemReveal(context, context.Main, context.Arg(4));
                    }
                    else if (effect?.StartsWith("item:") == true)
                    {
                        ItemReveal(context, context.Main, effect["item:".Length..]);
                    }
                    break;
                default:
                    if (from?.StartsWith("item:") == true)
                    {
                        var item = from["item:".Length..].Trim();
                        var holder = context.Main;
                        if (context.Command == "-damage" && context.Of is not null)
                        {
                            holder = context.Of;
                        }
                        else if (
                            context.Command == "-damage"
                            && AttackerDamagingItems.Contains(item)
                        )
                        {
                            holder = null;
                        }
                        ItemReveal(context, holder, item);
                    }
                    break;
            }
        }

        private static void HandleItemGain(LineContext context)
        {
            var item = context.Arg(3);
            var from = context.From;
            if (from is "move: Trick" or "move: Switcheroo")
            {
                var state = context.State;
                var originalHolder = SameIdent(context.Main, state.TrickUser)
                    ? state.TrickTarget
                    : state.TrickUser;
                var original = context.Resolve(originalHolder);
                if (original is not null && !state.ForeignItemHoldersBeforeTrick.Contains(original))
                {
                    ItemUpdate(original, item);
                }
                MarkForeignItemHolder(context, context.Main);
            }
            else if (from is not null && ItemTransferEffects.Contains(from))
            {
                ItemReveal(context, context.Of, item);
                MarkForeignItemHolder(context, context.Main);
            }
            else
            {
                // Air Balloon, Frisk, Harvest, Recycle, ...
                ItemReveal(context, context.Main, item);
            }
        }

        private static void StartItemSwap(LineContext context, string? user, string? target)
        {
            var state = context.State;
            state.TrickUser = user;
            state.TrickTarget = target;
            state.ForeignItemHoldersBeforeTrick = [.. state.ForeignItemHolders];
        }

        private static void MarkForeignItemHolder(LineContext context, string? ident)
        {
            var pokemon = context.Resolve(ident);
            if (pokemon is not null)
            {
                context.State.ForeignItemHolders.Add(pokemon);
            }
        }

        private static void ItemReveal(LineContext context, string? ident, string? item)
        {
            item = item?.Trim();
            if (string.IsNullOrEmpty(item))
            {
                return;
            }
            var pokemon = context.Resolve(ident);
            if (pokemon is not null && !context.State.ForeignItemHolders.Contains(pokemon))
            {
                ItemUpdate(pokemon, item);
            }
        }

        /// <summary>
        /// Abilities that copy the ability of the [of] Pokemon ("|-ability|p1a: Holder|Copied|[from] ability: Trace|[of] p2a: Other").
        /// </summary>
        private static readonly HashSet<string> CopyingAbilities =
        [
            "Trace",
            "Receiver",
            "Power of Alchemy"
        ];

        private static void HandleAbilities(LineContext context)
        {
            var command = context.Command;
            var from = context.From;
            var of = context.Of;
            string? ability = null;
            string? holder = null;

            if (from?.StartsWith("ability:") == true)
            {
                ability = from["ability:".Length..].Trim();
                if (command == "-ability" && CopyingAbilities.Contains(ability))
                {
                    holder = context.Main;
                    AbilityReveal(context, of, context.Arg(3));
                }
                else if (command == "-item")
                {
                    // Frisk reveals the item of the main Pokemon, Magician / Pickpocket steal it
                    holder = ability == "Frisk" ? of : context.Main;
                }
                else if (command == "-heal")
                {
                    // Absorbing abilities list the attacker as [of]
                    holder = ability == "Hospitality" ? of : context.Main;
                }
                else if (command == "-enditem")
                {
                    holder = SameIdent(of, context.Main) ? null : of;
                }
                else
                {
                    // "[from] ability: X|[of] p1a: Holder", e.g. weather, Flame Body, Toxic Chain
                    holder = of ?? context.Main;
                }
            }
            else
            {
                var abilityPart = context
                    .Parts.Skip(2)
                    .FirstOrDefault((part) => part.StartsWith("ability:"));
                if (abilityPart is not null)
                {
                    ability = abilityPart["ability:".Length..].Trim();
                    // "|-block|p1a: Target|ability: Flower Veil|[of] p1b: Holder"
                    holder = command == "-block" ? of ?? context.Main : context.Main;
                }
                else if (command == "-ability" && from is null)
                {
                    ability = context.Arg(3);
                    holder = context.Main;
                }
            }

            AbilityReveal(context, holder, ability);
        }

        private static void AbilityReveal(LineContext context, string? ident, string? ability)
        {
            ability = ability?.Trim();
            if (string.IsNullOrEmpty(ability))
            {
                return;
            }
            var pokemon = context.Resolve(ident);
            if (pokemon is not null)
            {
                AbilityUpdate(pokemon, ability);
            }
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
                    result ??= await _cache
                        .GetStringAsync($"{logLink}+{playerValue}")
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
                    .SetStringAsync($"{logLink}+{playerInfo.PlayerName}", serializedTeam)
                    .ConfigureAwait(false);
                await _cache
                    .SetStringAsync($"{logLink}+{playerInfo.PlayerValue}", serializedTeam)
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
                                if (
                                    LevenshteinDistance.Compute(playerInfo.PlayerName, user)
                                    > distance
                                )
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
                            if (
                                LevenshteinDistance.Compute(playerInfo.PlayerName, user) > distance
                                && !playerInfo.PlayerName.Contains(user)
                            )
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

        private static Pokemon AddMonIfNotExists(
            ICollection<Pokemon> pokemonList,
            string pokemonCandidate
        )
        {
            var pokemon = pokemonList.FirstOrDefault(
                (pokemon) =>
                    pokemon.Name == pokemonCandidate
                    || pokemon.FormName == pokemonCandidate
                    || pokemon.AltNames.Any((altName) => altName == pokemonCandidate)
            );
            if (pokemon == null)
            {
                var regexPokemonCandidate = RegexUtil.Regex(pokemonCandidate);
                pokemon = pokemonList.FirstOrDefault(
                    (pokemon) =>
                    {
                        return Common
                            .FormDescriptorList.Select(
                                (formDescriptor) =>
                                    RegexUtil.Regex($"{pokemon.Name}-{formDescriptor}")
                            )
                            .Any((possibleForm) => possibleForm == regexPokemonCandidate);
                    }
                );
                if (pokemon != null)
                {
                    pokemon.FormName = pokemonCandidate;
                    return pokemon;
                }
                pokemon = pokemonList.FirstOrDefault(
                    (pokemon) =>
                    {
                        return Common.FormPokemonList.Any(
                            (formPokemon) =>
                                formPokemon == pokemon.Name
                                && pokemonCandidate.Contains(formPokemon)
                        );
                    }
                );
                if (pokemon != null)
                {
                    pokemon.FormName = pokemonCandidate;
                }
                else
                {
                    pokemon = new Pokemon() { Name = pokemonCandidate };
                    pokemonList.Add(pokemon);
                }
            }
            return pokemon;
        }

        private static void AbilityUpdate(Pokemon pokemon, string ability)
        {
            pokemon.Ability = Common.MergeAlternatives(pokemon.Ability, ability);
        }

        private static void TeraUpdate(Pokemon pokemon, string teraType)
        {
            pokemon.TeraType = Common.MergeAlternatives(pokemon.TeraType, teraType);
        }

        private static void ItemUpdate(Pokemon pokemon, string? item)
        {
            pokemon.Item = Common.MergeAlternatives(pokemon.Item, item);
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
        public IDictionary<string, string> ZStatusMoveItems = new Dictionary<string, string>
        {
            { "Metronome", "Normalium Z" },
        };

        private void MoveUpdate(Pokemon pokemon, string move)
        {
            if (move.StartsWith("Z-") && move.Length > 2)
            {
                // Z-powered status moves, e.g. "Z-Metronome"
                move = move[2..];
                if (ZStatusMoveItems.TryGetValue(move, out string? zCrystal))
                {
                    ItemUpdate(pokemon, zCrystal);
                }
            }
            if (ItemTransformingMoves.TryGetValue(move, out string? value))
            {
                ItemUpdate(pokemon, value);
            }
            else if (!pokemon.Moves.Contains(move) && !IllegalMoves.Contains(move))
            {
                pokemon.Moves.Add(move);
            }
        }
    }
}
