using System.Collections.Generic;
using System.Linq;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis.Handlers
{
    /// <summary>
    /// Builds the team: team preview, switches (identifying Pokemon by nickname), forms and tera types.
    /// </summary>
    internal sealed class TeamHandler : ILogLineHandler
    {
        /// <summary>
        /// The active Pokemon per position and its state when it switched in,
        /// used to move what a disguised Zoroark revealed to the Zoroark.
        /// </summary>
        private readonly Dictionary<string, (Pokemon Pokemon, Pokemon SwitchInState)> _active = [];

        /// <summary>
        /// The nickname a Pokemon was first identified by.
        /// </summary>
        private readonly Dictionary<Pokemon, string> _nicknameOwners = [];

        public void Handle(ProtocolLine line, AnalysisContext context)
        {
            switch (line.Command)
            {
                case "poke":
                    HandlePoke(line, context);
                    break;
                case "switch":
                case "drag":
                case "replace":
                    HandleSwitch(line, context);
                    break;
                case "detailschange":
                    HandleDetailsChange(line, context);
                    break;
                case "-terastallize":
                    HandleTerastallize(line, context);
                    break;
            }
        }

        private static void HandlePoke(ProtocolLine line, AnalysisContext context)
        {
            var details = line.Arg(3);
            if (details is not null && line.Arg(2) == context.PlayerInfo.PlayerValue)
            {
                context.Team.Pokemon.Add(
                    new Pokemon()
                    {
                        Name = details.Split(',')[0],
                        AltNames = { details.Split('-')[0] }
                    }
                );
            }
        }

        private void HandleSwitch(ProtocolLine line, AnalysisContext context)
        {
            var details = line.Arg(3);
            if (details is null)
            {
                // Something broke in the replay, skipping
                return;
            }
            if (!context.IsOwn(line.Arg(2), out var position, out var nickname))
            {
                return;
            }

            var pokemon = FindSwitchedInPokemon(context, nickname, details.Split(',')[0]);
            _nicknameOwners.TryAdd(pokemon, nickname);
            if (
                line.Command == "replace"
                && _active.TryGetValue(position, out var disguise)
                && disguise.Pokemon != pokemon
            )
            {
                // Illusion ended, everything revealed while disguised belongs to the Zoroark
                TransferRevealedInformation(disguise.Pokemon, disguise.SwitchInState, pokemon);
            }
            _active[position] = (pokemon, pokemon.Clone());
        }

        private Pokemon FindSwitchedInPokemon(
            AnalysisContext context,
            string nickname,
            string species
        )
        {
            var team = context.Team;
            var nicknames = context.Nicknames;
            // Old replays allowed the same nickname for several Pokemon, so the nickname
            // only identifies the Pokemon if the species matches
            if (
                nicknames.TryGetValue(nickname, out var pokemon)
                && SpeciesForms.IsSameSpecies(species, pokemon)
            )
            {
                if (pokemon.Name != species && pokemon.FormName != species)
                {
                    pokemon.FormName = species;
                }
                return pokemon;
            }

            // Pokemon already identified by another nickname are a different Pokemon,
            // this keeps e.g. two Heracross in the same team apart
            var unclaimed = team
                .Pokemon.Where(
                    (pokemon) =>
                        !_nicknameOwners.TryGetValue(pokemon, out var owner) || owner == nickname
                )
                .ToList();
            pokemon = unclaimed.FirstOrDefault(
                (pokemon) => pokemon.Name == species || pokemon.FormName == species
            );
            if (pokemon == null)
            {
                pokemon = unclaimed.FirstOrDefault(
                    (pokemon) => SpeciesForms.IsFormOf(species, pokemon.Name)
                );
                pokemon ??= unclaimed.FirstOrDefault(
                    (pokemon) =>
                        pokemon.Name == nickname
                        || pokemon.FormName == nickname
                        || pokemon.AltNames.Any((altName) => altName == nickname)
                );
                if (pokemon != null)
                {
                    pokemon.FormName = species;
                }
                else
                {
                    pokemon = new Pokemon() { Name = species };
                    if (team.Pokemon.Count == 0)
                    {
                        pokemon.Lead = true;
                    }
                    team.Pokemon.Add(pokemon);
                }
            }

            nicknames[nickname] = pokemon;
            if (!pokemon.AltNames.Any((altName) => altName == nickname))
            {
                pokemon.AltNames.Add(nickname);
            }
            return pokemon;
        }

        private static void TransferRevealedInformation(
            Pokemon disguise,
            Pokemon disguiseBefore,
            Pokemon actual
        )
        {
            foreach (var move in disguise.Moves.Except(disguiseBefore.Moves).ToList())
            {
                disguise.Moves.Remove(move);
                if (!actual.Moves.Contains(move))
                {
                    actual.Moves.Add(move);
                }
            }
            actual.RevealItem(Alternatives.Added(disguiseBefore.Item, disguise.Item));
            disguise.Item = disguiseBefore.Item;
            actual.Ability = Alternatives.Merge(
                actual.Ability,
                Alternatives.Added(disguiseBefore.Ability, disguise.Ability)
            );
            disguise.Ability = disguiseBefore.Ability;
        }

        private static void HandleDetailsChange(ProtocolLine line, AnalysisContext context)
        {
            var species = line.Arg(3)?.Split(',')[0];
            var pokemon = context.Resolve(line.Arg(2));
            if (pokemon is null || species is null)
            {
                return;
            }
            if (pokemon.FormName != species)
            {
                pokemon.FormName = species;
            }
            // Old replays show a Mega Evolution only as details change, without "|-mega|"
            if (MegaStones.Items.TryGetValue(species, out var megaStone))
            {
                pokemon.RevealItem(megaStone);
            }
        }

        private static void HandleTerastallize(ProtocolLine line, AnalysisContext context)
        {
            var teraType = line.Arg(3)?.Trim();
            var pokemon = context.Resolve(line.Arg(2));
            if (pokemon is not null && !string.IsNullOrEmpty(teraType))
            {
                pokemon.RevealTeraType(teraType);
            }
        }
    }
}
