using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.Data
{
    [Serializable]
    public class Team
    {
        public ICollection<Replay> Replays { get; set; }
        public ICollection<Pokemon> Pokemon { get; set; }
        public string? Format { get; set; }

        public Team()
        {
            Replays = new List<Replay>();
            Pokemon = new List<Pokemon>();
        }

        public Team Clone()
        {
            return new Team()
            {
                Replays = Replays.Select((replay) => replay.Clone()).ToList(),
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
            return Replays.Count > 0 && Pokemon.Count > 0;
        }

        public override string ToString()
        {
            return string.Join(", ", OrderedPokemonNames());
        }
    }
}
