using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.Data
{
    public class Team
    {
        public ICollection<Uri> Links { get; set; } = new List<Uri>();
        public ICollection<Pokemon> Pokemon { get; set; } = new List<Pokemon>();

        public Team Clone()
        {
            return new Team()
            {
                Links = Links.Select((link) => link).ToList(),
                Pokemon = Pokemon.Select((pokemon) => pokemon.Clone()).ToList()
            };
        }

        public IEnumerable<string> OrderedPokemonNames()
        {
            return Pokemon
                .Select((pokemon) => pokemon.ToString())
                .OrderBy((pokemonName) => pokemonName);
        }

        public override string ToString()
        {
            return string.Join(", ", OrderedPokemonNames());
        }
    }
}
