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
                Pokemon = Pokemon.Select((pokemon) => pokemon.Clone()).ToList()
            };
        }

        public override string ToString()
        {
            var pokemonNames = Pokemon.Select((pokemon) => (pokemon.Lead ? "(Lead) " : "") + ((pokemon.FormName != null) ? pokemon.FormName : pokemon.Name));
            return string.Join(", ", pokemonNames);
        }
    }
}
