using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers
{
    public class ShowdownReplayAnalyzer : IReplayAnalyzer
    {
        public IEnumerable<Team> AnalyzeReplay(string replay)
        {
            return AnalyzeReplayAsync(replay).Result;
        }

        public IEnumerable<Team> AnalyzeReplay(Uri replay)
        {
            return AnalyzeReplayAsync(replay).Result;
        }

        public IEnumerable<Team> AnalyzeReplay(string replay, string user)
        {
            return AnalyzeReplayAsync(replay, user).Result;
        }

        public IEnumerable<Team> AnalyzeReplay(Uri replay, string user)
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

        public async Task<IEnumerable<Team>> AnalyzeReplayAsync(string replay, string user)
        {
            return await AnalyzeReplayAsync(new Uri(replay), user).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Team>> AnalyzeReplayAsync(Uri replay, string user)
        {
            if (user == null)
            {
                return await AnalyzeReplayAsync(replay).ConfigureAwait(false);
            }
            return new List<Team> {
                await GetTeamFromUrl(replay, user).ConfigureAwait(false)
            };
        }

        private static async Task<Team> GetTeamFromUrl(Uri link, string user = null, string playerValue = "")
        {
            var replay = await Common.HttpClient.GetStringAsync(link).ConfigureAwait(false);

            var playerName = "";

            var team = new Team()
            {
                Links = new List<Uri>
                {
                    link
                }
            };

            foreach (var line in replay.Split('\n'))
            {
                if (line.Contains("|player"))
                {
                    DeterminePlayer(user, ref playerValue, ref playerName, line);
                }
                else if (playerValue?.Length == 0)
                {
                    continue;
                }
                else if (line.Contains("|poke|"))
                {
                    var pokeinf = line.Split('|');
                    if (pokeinf[2] == playerValue)
                    {
                        team.Pokemon.Add(new Pokemon()
                        {
                            Name = pokeinf[3].Split(',')[0]
                        });
                    }
                }
                else if (line.Contains("|switch") || line.Contains("|drag"))
                {
                    if (line.Contains(playerValue))
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
                        if (playerString.Contains(playerValue))
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
                                if (!pokemon.Moves.Contains(move))
                                {
                                    pokemon.Moves.Add(move);
                                }
                            }
                        }
                    }
                }
                else if (line.Contains("|detailschange"))
                {
                    var detailinf = line.Split('|');
                    if (detailinf[2].Contains(playerValue))
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

                        if (playerString.Contains(playerValue))
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
                    if (iteminf.Length > 5 && iteminf[5].Contains(playerValue))
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

                        if (playerString.Contains(playerValue))
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
                        if (abilityinf[5].Split(':')[1].Contains(playerValue))
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
                        else if (abinf.Contains(':') && abinf.Split(':')[0].Contains(playerValue))
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
                    if (abilityinf[2].Contains(':') && abilityinf[2].Split(':')[0].Contains(playerValue))
                    {
                        var ability = abilityinf[3].Trim();
                        var mon = abilityinf[2].Split(':')[1].Trim();

                        var pokemon = AddMonIfNotExists(team.Pokemon, mon);
                        AbilityUpdate(pokemon, ability);
                    }
                }
            }

            return team;
        }

        private static void DeterminePlayer(string rawUser, ref string playerValue, ref string playerName, string line)
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
                        if (playerName != "")
                        {
                            if (playerName.Contains(user))
                            {
                                if (LevenshteinDistance.Compute(playerName, user) > distance)
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
                        if (playerName != "")
                        {
                            if (LevenshteinDistance.Compute(playerName, user) > distance && !playerName.Contains(user))
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
                playerValue = playerinf[2];
                playerName = regexedPlayerInf;
            }

            if (playerValue == playerinf[2] && playerName?.Length == 0)
            {
                playerName = regexedPlayerInf;
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

        private static void ItemUpdate(Pokemon pokemon, string item)
        {
            if (pokemon.Item == null || pokemon.Item?.Length == 0)
            {
                pokemon.Item = item;
            }
            else
            {
                if (!pokemon.Item.Contains(item))
                {
                    pokemon.Item += " | " + item;
                }
            }
        }
    }
}
