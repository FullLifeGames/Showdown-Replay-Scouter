using System;
using System.Collections.Generic;
using System.Linq;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis
{
    /// <summary>
    /// The team of the scouted player that is built up while analyzing a replay log.
    /// </summary>
    internal sealed class AnalysisContext(
        Team team,
        PlayerInfo playerInfo,
        Replay? replay,
        MoveRules moveRules
    )
    {
        public Team Team { get; } = team;
        public PlayerInfo PlayerInfo { get; } = playerInfo;
        public Replay? Replay { get; } = replay;
        public MoveRules MoveRules { get; } = moveRules;

        /// <summary>
        /// Maps the nickname used in idents ("p1a: Nickname") to the Pokemon.
        /// </summary>
        public Dictionary<string, Pokemon> Nicknames { get; } = [];

        /// <summary>
        /// Whether the ident ("p1a: Nick") belongs to the scouted player.
        /// </summary>
        public bool IsOwn(string? ident, out string position, out string nickname)
        {
            return PokemonIdent.TryParse(ident, out position, out nickname)
                && position.StartsWith(PlayerInfo.PlayerValue!);
        }

        /// <summary>
        /// Returns the Pokemon of the scouted player for an ident, adding it if it is not known yet.
        /// </summary>
        public Pokemon? Resolve(string? ident)
        {
            if (!IsOwn(ident, out _, out var nickname))
            {
                return null;
            }
            return Nicknames.TryGetValue(nickname, out var pokemon)
                ? pokemon
                : AddMonIfNotExists(nickname);
        }

        /// <summary>
        /// Returns the already identified Pokemon of the scouted player for an ident.
        /// </summary>
        public Pokemon? Find(string? ident)
        {
            return IsOwn(ident, out _, out var nickname)
                ? Nicknames.GetValueOrDefault(nickname)
                : null;
        }

        private Pokemon AddMonIfNotExists(string pokemonCandidate)
        {
            var pokemonList = Team.Pokemon;
            var pokemon = pokemonList.FirstOrDefault(
                (pokemon) =>
                    pokemon.Name == pokemonCandidate
                    || pokemon.FormName == pokemonCandidate
                    || pokemon.AltNames.Any((altName) => altName == pokemonCandidate)
            );
            if (pokemon == null)
            {
                // A form of a known Pokemon, e.g. "Zoroark-Hisui" or "Tauros-Paldea-Combat"
                pokemon = pokemonList.FirstOrDefault(
                    (pokemon) => SpeciesForms.IsFormOf(pokemonCandidate, pokemon.Name)
                );
                if (pokemon != null)
                {
                    pokemon.FormName = pokemonCandidate;
                }
                else
                {
                    pokemon = new Pokemon() { Name = pokemonCandidate };
                    pokemonList.Add(pokemon);
                }
            }
            return pokemon;
        }
    }
}
