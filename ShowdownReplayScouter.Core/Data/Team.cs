using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.Data
{
    public class Team
    {
        public ICollection<Uri> Links { get; set; } = new List<Uri>();
        public ICollection<Pokemon> Pokemon { get; set; } = new List<Pokemon>();
        public string? Format { get; set; }

        public Team Clone()
        {
            return new Team()
            {
                Links = Links.Select((link) => link).ToList(),
                Pokemon = Pokemon.Select((pokemon) => pokemon.Clone()).ToList(),
                Format = Format
            };
        }

        public IEnumerable<string> OrderedPokemonNames()
        {
            return Pokemon
                .Select((pokemon) => pokemon.ToString())
                .OrderBy((pokemonName) => pokemonName);
        }

        public bool IsValid()
        {
            return Links.Count > 0 && Pokemon.Count > 0;
        }

        public override string ToString()
        {
            return string.Join(", ", OrderedPokemonNames());
        }
    }
}
