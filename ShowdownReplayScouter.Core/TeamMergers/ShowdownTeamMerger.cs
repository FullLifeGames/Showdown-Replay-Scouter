using ShowdownReplayScouter.Core.Data;
using System.Linq;
using System.Collections.Generic;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.TeamMergers
{
    public class ShowdownTeamMerger : ITeamMerger
    {
        public IEnumerable<Team> MergeTeams(IEnumerable<Team> teams)
        {
            var returnList = new List<Team>();
            foreach (var definition in teams.Select((team) => team.ToString()).Distinct())
            {
                var definitionTeams = teams.Where((team) => team.ToString() == definition);
                var team = new Team();
                foreach (var definitionTeam in definitionTeams)
                {
                    team = MergeTeams(team, definitionTeam);
                }
                returnList.Add(team);
            }

            foreach (var returnEntry in returnList)
            {
                returnEntry.Pokemon = returnEntry.Pokemon
                    .OrderBy((pokemon) => pokemon.ToString())
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

            foreach (var pokemon in team2.Pokemon)
            {
                var foundPokemon = team.Pokemon.FirstOrDefault((pokemonEntry) => pokemonEntry.Name == pokemon.Name);
                if (foundPokemon is null)
                {
                    team.Pokemon.Add(pokemon);
                }
                else
                {
                    if (foundPokemon.Name == null && pokemon.Name != null)
                    {
                        foundPokemon.Name = pokemon.Name;
                    }
                    if (foundPokemon.FormName == null && pokemon.FormName != null)
                    {
                        foundPokemon.FormName = pokemon.FormName;
                    }
                    if (foundPokemon.Item == null && pokemon.Item != null)
                    {
                        foundPokemon.Item = pokemon.Item;
                    }
                    if (foundPokemon.Ability == null && pokemon.Ability != null)
                    {
                        foundPokemon.Ability = pokemon.Ability;
                    }
                    if (foundPokemon.Lead == false && pokemon.Lead != false)
                    {
                        foundPokemon.Lead = pokemon.Lead;
                    }
                    if (foundPokemon.TeraType == null && pokemon.TeraType != null)
                    {
                        foundPokemon.TeraType = pokemon.TeraType;
                    }
                    else if (foundPokemon.TeraType != null && pokemon.TeraType != null)
                    {
                        foundPokemon.TeraType += " | " + pokemon.TeraType;
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
    }
}
