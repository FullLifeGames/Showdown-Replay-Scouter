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

        private static Pokemon FindSwitchedInPokemon(
            AnalysisContext context,
            string nickname,
            string species
        )
        {
            var team = context.Team;
            var nicknames = context.Nicknames;
            if (nicknames.TryGetValue(nickname, out var pokemon))
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
                .Pokemon.Where((pokemon) => !nicknames.ContainsValue(pokemon))
                .ToList();
            pokemon = unclaimed.FirstOrDefault(
                (pokemon) => pokemon.Name == species || pokemon.FormName == species
            );
            if (pokemon == null)
            {
                pokemon = unclaimed.FirstOrDefault(
                    (pokemon) =>
                        Common.FormPokemonList.Any(
                            (formPokemon) =>
                                formPokemon == pokemon.Name && species.Contains(formPokemon)
                        )
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
            actual.RevealItem(AddedAlternatives(disguiseBefore.Item, disguise.Item));
            disguise.Item = disguiseBefore.Item;
            actual.Ability = Common.MergeAlternatives(
                actual.Ability,
                AddedAlternatives(disguiseBefore.Ability, disguise.Ability)
            );
            disguise.Ability = disguiseBefore.Ability;
        }

        private static string? AddedAlternatives(string? before, string? after)
        {
            if (string.IsNullOrEmpty(after))
            {
                return null;
            }
            var previous = before?.Split(Common.AlternativeSeparator) ?? [];
            var added = after.Split(Common.AlternativeSeparator).Except(previous).ToList();
            return added.Count > 0 ? string.Join(Common.AlternativeSeparator, added) : null;
        }

        private static void HandleDetailsChange(ProtocolLine line, AnalysisContext context)
        {
            var species = line.Arg(3)?.Split(',')[0];
            var pokemon = context.Resolve(line.Arg(2));
            if (pokemon is not null && species is not null && pokemon.FormName != species)
            {
                pokemon.FormName = species;
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
