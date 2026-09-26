using System.Collections.Generic;
using ShowdownReplayScouter.Core.Data;

namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis.Handlers
{
    /// <summary>
    /// Tracks the items the Pokemon brought, ignoring items received by Trick, Thief, ...
    /// </summary>
    internal sealed class ItemHandler : ILogLineHandler
    {
        /// <summary>
        /// Items that hurt the attacker, so the damaged Pokemon is not the holder.
        /// </summary>
        private static readonly HashSet<string> AttackerDamagingItems =
        [
            "Rocky Helmet",
            "Jaboca Berry",
            "Rowap Berry"
        ];

        /// <summary>
        /// Effects that give a Pokemon an item it did not bring, the [of] Pokemon (if any) brought it.
        /// </summary>
        private static readonly HashSet<string> ItemTransferEffects =
        [
            "move: Thief",
            "move: Covet",
            "move: Bestow",
            "ability: Magician",
            "ability: Pickpocket",
            "ability: Pickup"
        ];

        /// <summary>
        /// Pokemon currently holding an item they did not bring (e.g. after Trick).
        /// </summary>
        private readonly HashSet<Pokemon> _foreignItemHolders = [];

        private string? _swapUser;
        private string? _swapTarget;
        private HashSet<Pokemon> _foreignItemHoldersBeforeSwap = [];

        public void Handle(ProtocolLine line, AnalysisContext context)
        {
            switch (line.Command)
            {
                case "move":
                    if (line.Arg(3) is "Trick" or "Switcheroo")
                    {
                        StartItemSwap(line.Arg(2), line.Arg(4));
                    }
                    break;
                case "-item":
                    HandleItemGain(line, context);
                    break;
                case "-enditem":
                    RevealItem(context, line.Main, line.Arg(3));
                    break;
                case "-mega":
                case "-burst":
                    // "|-mega|p1a: Nick|Venusaur|Venusaurite", "|-burst|p1a: Nick|Necrozma-Ultra|Ultranecrozium Z"
                    RevealItem(context, line.Main, line.Arg(4));
                    break;
                case "-activate":
                    HandleActivate(line, context);
                    break;
                default:
                    if (line.IsEffect)
                    {
                        HandleItemEffect(line, context);
                    }
                    break;
            }
        }

        private void HandleActivate(ProtocolLine line, AnalysisContext context)
        {
            var effect = line.Arg(3)?.Trim();
            if (effect is "move: Trick" or "move: Switcheroo")
            {
                StartItemSwap(line.Main, line.Of);
            }
            else if (effect == "move: Poltergeist")
            {
                // "|-activate|p1a: Target|move: Poltergeist|Item" reveals the item of the target
                RevealItem(context, line.Main, line.Arg(4));
            }
            else if (effect?.StartsWith("item:") == true)
            {
                RevealItem(context, line.Main, effect["item:".Length..]);
            }
        }

        /// <summary>
        /// "[from] item: Leftovers", for damage the [of] Pokemon holds e.g. the Rocky Helmet.
        /// </summary>
        private void HandleItemEffect(ProtocolLine line, AnalysisContext context)
        {
            var from = line.From;
            if (from?.StartsWith("item:") != true)
            {
                return;
            }
            var item = from["item:".Length..].Trim();
            var holder = line.Main;
            if (line.Command == "-damage" && line.Of is not null)
            {
                holder = line.Of;
            }
            else if (line.Command == "-damage" && AttackerDamagingItems.Contains(item))
            {
                holder = null;
            }
            RevealItem(context, holder, item);
        }

        private void HandleItemGain(ProtocolLine line, AnalysisContext context)
        {
            var item = line.Arg(3);
            var from = line.From;
            if (from is "move: Trick" or "move: Switcheroo")
            {
                var originalHolder = PokemonIdent.AreSame(line.Main, _swapUser)
                    ? _swapTarget
                    : _swapUser;
                var original = context.Resolve(originalHolder);
                if (original is not null && !_foreignItemHoldersBeforeSwap.Contains(original))
                {
                    original.RevealItem(item);
                }
                MarkForeignItemHolder(context, line.Main);
            }
            else if (from is not null && ItemTransferEffects.Contains(from))
            {
                RevealItem(context, line.Of, item);
                MarkForeignItemHolder(context, line.Main);
            }
            else
            {
                // Air Balloon, Frisk, Harvest, Recycle, ...
                RevealItem(context, line.Main, item);
            }
        }

        private void StartItemSwap(string? user, string? target)
        {
            _swapUser = user;
            _swapTarget = target;
            _foreignItemHoldersBeforeSwap = [.. _foreignItemHolders];
        }

        private void MarkForeignItemHolder(AnalysisContext context, string? ident)
        {
            var pokemon = context.Resolve(ident);
            if (pokemon is not null)
            {
                _foreignItemHolders.Add(pokemon);
            }
        }

        private void RevealItem(AnalysisContext context, string? ident, string? item)
        {
            item = item?.Trim();
            if (string.IsNullOrEmpty(item))
            {
                return;
            }
            var pokemon = context.Resolve(ident);
            if (pokemon is not null && !_foreignItemHolders.Contains(pokemon))
            {
                pokemon.RevealItem(item);
            }
        }
    }
}
