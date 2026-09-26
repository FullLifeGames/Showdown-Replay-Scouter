using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ShowdownReplayScouter.Core.Util
{
    public sealed record ShowdownSet(
        string? Nickname,
        string Species,
        string? Item,
        string? Ability,
        string? TeraType,
        IReadOnlyList<string> Moves
    );

    /// <summary>
    /// Parses the sets posted into the battle chat via "!showteam" / "!showset",
    /// which are rendered as "/raw" HTML containing the Showdown export format.
    /// </summary>
    public static partial class ShowdownSetParser
    {
        public static bool IsPostedSets(string chatMessage)
        {
            return chatMessage.StartsWith("/raw ") && chatMessage.Contains("<summary>View ");
        }

        public static IEnumerable<ShowdownSet> ParseRawHtml(string html)
        {
            var content = html;
            var summaryEnd = content.IndexOf("</summary>");
            if (summaryEnd >= 0)
            {
                content = content[(summaryEnd + "</summary>".Length)..];
            }
            var detailsEnd = content.IndexOf("</details>");
            if (detailsEnd >= 0)
            {
                content = content[..detailsEnd];
            }

            var setLines = new List<string>();
            foreach (var rawLine in content.Split("<br />"))
            {
                var line = WebUtility.HtmlDecode(HtmlTagRegex().Replace(rawLine, "")).Trim();
                if (line.Length == 0)
                {
                    if (setLines.Count > 0)
                    {
                        yield return ParseSet(setLines);
                        setLines.Clear();
                    }
                    continue;
                }
                setLines.Add(line);
            }
            if (setLines.Count > 0)
            {
                yield return ParseSet(setLines);
            }
        }

        private static ShowdownSet ParseSet(IReadOnlyList<string> lines)
        {
            var header = lines[0];
            string? item = null;
            var itemSeparator = header.LastIndexOf(" @ ");
            if (itemSeparator >= 0)
            {
                item = header[(itemSeparator + 3)..].Trim();
                header = header[..itemSeparator].Trim();
            }
            if (header.EndsWith(" (M)") || header.EndsWith(" (F)"))
            {
                header = header[..^4].TrimEnd();
            }

            string? nickname = null;
            var species = header;
            var speciesStart = header.LastIndexOf(" (");
            if (header.EndsWith(')') && speciesStart > 0)
            {
                nickname = header[..speciesStart].Trim();
                species = header[(speciesStart + 2)..^1].Trim();
            }

            string? ability = null;
            string? teraType = null;
            var moves = new List<string>();
            foreach (var line in lines.Skip(1))
            {
                if (line.StartsWith("Ability:"))
                {
                    ability = line["Ability:".Length..].Trim();
                }
                else if (line.StartsWith("Tera Type:"))
                {
                    teraType = line["Tera Type:".Length..].Trim();
                }
                else if (line.StartsWith("- "))
                {
                    moves.Add(line[2..].Trim());
                }
            }

            return new ShowdownSet(nickname, species, item, ability, teraType, moves);
        }

        [GeneratedRegex("<[^>]+>")]
        private static partial Regex HtmlTagRegex();
    }
}
