using System.Collections.Generic;
using System.Linq;
using ShowdownReplayScouter.Core.Data;

namespace ShowdownReplayScouter.Core.Util
{
    public class TeamPrinter
    {
        public static IEnumerable<string> Print(IEnumerable<Team> teams)
        {
            return teams.Select(Print);
        }

        public static string Print(Team team)
        {
            var pokemonRepresentation = "";
            foreach (var pokemon in team.Pokemon.OrderBy((pokemon) => pokemon.ToString()))
            {
                pokemonRepresentation +=
                    (pokemon.FormName ?? pokemon.Name)
                    + ((pokemon.Item != null) ? " @ " + pokemon.Item : "")
                    + "\r\n";
                if (pokemon.Ability != null)
                    pokemonRepresentation += "Ability: " + pokemon.Ability + "\r\n";
                if (pokemon.TeraType != null)
                    pokemonRepresentation += "Tera Type: " + pokemon.TeraType + "\r\n";
                foreach (var move in pokemon.Moves.OrderBy((move) => move))
                {
                    pokemonRepresentation += "- " + move + "\r\n";
                }
                pokemonRepresentation += "\r\n";
            }
            return pokemonRepresentation;
        }
    }
}
