using System.Collections.Generic;
using System.Linq;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.TeamMergers
{
    public class ShowdownTeamMerger : ITeamMerger
    {
        public IEnumerable<Team> MergeTeams(IEnumerable<Team> teams)
        {
            var returnList = new List<Team>();
            foreach (var definitionTeams in teams.GroupBy((team) => team.ToString()))
            {
                var team = new Team();
                foreach (var definitionTeam in definitionTeams)
                {
                    team = MergeTeams(team, definitionTeam) ?? team;
                }
                returnList.Add(team);
            }

            foreach (var returnEntry in returnList)
            {
                returnEntry.Pokemon = returnEntry
                    .Pokemon.OrderBy((pokemon) => pokemon.ToString())
                    .ToList();
            }

            returnList.Sort(new TeamComparer());

            return returnList;
        }

        public Team? MergeTeams(Team? team1, Team? team2)
        {
            if (team1 == null && team2 == null)
            {
                return null;
            }
            else if (team1 == null)
            {
                return team2!.Clone();
            }
            else if (team2 == null)
            {
                return team1!.Clone();
            }

            var team = team1.Clone();

            var matchedPokemon = new HashSet<Pokemon>();
            foreach (var pokemon in team2.Pokemon)
            {
                var foundPokemon = FindMatchingPokemon(team.Pokemon, pokemon, matchedPokemon);
                if (foundPokemon is null)
                {
                    var addedPokemon = pokemon.Clone();
                    team.Pokemon.Add(addedPokemon);
                    matchedPokemon.Add(addedPokemon);
                }
                else
                {
                    matchedPokemon.Add(foundPokemon);
                    if (foundPokemon.FormName == null && pokemon.FormName != null)
                    {
                        foundPokemon.FormName = pokemon.FormName;
                    }
                    foundPokemon.Item = Alternatives.Merge(foundPokemon.Item, pokemon.Item);
                    foundPokemon.Ability = Alternatives.Merge(
                        foundPokemon.Ability,
                        pokemon.Ability
                    );
                    foundPokemon.TeraType = Alternatives.Merge(
                        foundPokemon.TeraType,
                        pokemon.TeraType
                    );
                    if (!foundPokemon.Lead && pokemon.Lead)
                    {
                        foundPokemon.Lead = pokemon.Lead;
                    }
                    foreach (var move in pokemon.Moves)
                    {
                        if (!foundPokemon.Moves.Contains(move))
                        {
                            foundPokemon.Moves.Add(move);
                        }
                    }
                    foreach (var altName in pokemon.AltNames)
                    {
                        if (!foundPokemon.AltNames.Contains(altName))
                        {
                            foundPokemon.AltNames.Add(altName);
                        }
                    }
                }
            }

            foreach (var otherReplay in team2.Replays)
            {
                if (!team.Replays.Any((replay) => replay.Link == otherReplay.Link))
                {
                    team.Replays.Add(otherReplay);
                }
            }

            return team;
        }

        /// <summary>
        /// Finds the Pokemon of the same species that has not been merged yet,
        /// preferring one with a shared nickname, so the same species twice stays two Pokemon.
        /// </summary>
        private static Pokemon? FindMatchingPokemon(
            IEnumerable<Pokemon> pokemonList,
            Pokemon pokemon,
            ISet<Pokemon> matchedPokemon
        )
        {
            var candidates = pokemonList
                .Where(
                    (pokemonEntry) =>
                        pokemonEntry.Name == pokemon.Name && !matchedPokemon.Contains(pokemonEntry)
                )
                .ToList();
            return candidates.FirstOrDefault(
                    (pokemonEntry) => pokemonEntry.AltNames.Intersect(pokemon.AltNames).Any()
                ) ?? candidates.FirstOrDefault();
        }
    }
}
