using ShowdownReplayScouter.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.Util
{
    public class TeamPrinter
    {
        public static IEnumerable<string> Print(IEnumerable<Team> teams)
        {
            return teams.Select((team) => Print(team));
        }

        public static string Print(Team team)
        {
            string pokemonRepresentation = "";
            foreach (var pokemon in team.Pokemon)
            {
                pokemonRepresentation += ((pokemon.FormName != null) ? pokemon.FormName : pokemon.Name) + ((pokemon.Item != null) ? " @ " + pokemon.Item : "") + "\r\n";
                if (pokemon.Ability != null) pokemonRepresentation += "Ability: " + pokemon.Ability + "\r\n";
                foreach (string move in pokemon.Moves)
                {
                    pokemonRepresentation += "- " + move + "\r\n";
                }
                pokemonRepresentation += "\r\n";
            }
            return pokemonRepresentation;
        }
    }
}
