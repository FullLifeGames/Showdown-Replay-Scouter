using System.Collections.Generic;
using System.Linq;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis.Handlers
{
    /// <summary>
    /// Sets posted with "!showteam" / "!showset" by the scouted player are the actual sets.
    /// </summary>
    internal sealed class PostedSetHandler : ILogLineHandler
    {
        private readonly HashSet<Pokemon> _postedSets = [];

        public void Handle(ProtocolLine line, AnalysisContext context)
        {
            if (line.Command != "c" || line.Parts.Length < 4)
            {
                return;
            }
            var message = line.ArgsFrom(3);
            if (!ShowdownSetParser.IsPostedSets(message) || !IsScoutedPlayer(line.Arg(2), context))
            {
                return;
            }

            // Only "!showteam" is clearly marked, other posts must match a Pokemon of the team
            var onlyKnownPokemon = !ShowdownSetParser.IsPostedTeam(message);
            foreach (var set in ShowdownSetParser.ParseRawHtml(message))
            {
                ApplyPostedSet(context, set, onlyKnownPokemon);
            }
        }

        private static bool IsScoutedPlayer(string? sender, AnalysisContext context)
        {
            var regexSender = RegexUtil.Regex(sender);
            return regexSender.Length > 0
                && regexSender == RegexUtil.Regex(context.PlayerInfo.PlayerName);
        }

        private void ApplyPostedSet(AnalysisContext context, ShowdownSet set, bool onlyKnownPokemon)
        {
            var pokemon = FindPokemon(context, set);
            if (pokemon is null)
            {
                if (onlyKnownPokemon)
                {
                    return;
                }
                pokemon = new Pokemon() { Name = set.Species };
                context.Team.Pokemon.Add(pokemon);
            }
            if (
                set.Nickname is not null
                && !pokemon.AltNames.Any((altName) => altName == set.Nickname)
            )
            {
                pokemon.AltNames.Add(set.Nickname);
            }

            _postedSets.Add(pokemon);
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

        private Pokemon? FindPokemon(AnalysisContext context, ShowdownSet set)
        {
            if (
                set.Nickname is not null
                && context.Nicknames.TryGetValue(set.Nickname, out var pokemon)
            )
            {
                return pokemon;
            }
            var candidates = context
                .Team.Pokemon.Where((pokemon) => !_postedSets.Contains(pokemon))
                .ToList();
            return candidates.FirstOrDefault(
                    (pokemon) =>
                        set.Nickname is not null
                        && pokemon.AltNames.Any((altName) => altName == set.Nickname)
                )
                ?? candidates.FirstOrDefault(
                    (pokemon) =>
                        IsSpecies(pokemon, set.Species) && !context.Nicknames.ContainsValue(pokemon)
                )
                ?? candidates.FirstOrDefault((pokemon) => IsSpecies(pokemon, set.Species));
        }

        private static bool IsSpecies(Pokemon pokemon, string species)
        {
            return pokemon.Name == species || pokemon.FormName == species;
        }
    }
}
