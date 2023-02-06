using ShowdownReplayScouter.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.Util
{
    public static class OutputPrinter
    {
        public static string Print(ScoutingRequest scoutingRequest, IEnumerable<Team> teams)
        {
            var output = "";
            if (scoutingRequest.Users != null)
            {
                output += string.Join(", ", scoutingRequest.Users);
            }
            output += scoutingRequest.Tiers != null ? $" ({string.Join(", ", scoutingRequest.Tiers)})" : "";
            output += ":\r\n\r\n";

            foreach (var team in teams)
            {
                if (team.IsValid())
                {
                    output += $"{team}:\r\n";
                    output += string.Join("\r\n", team.Replays.Select((replay) => replay.Link));
                    output += "\r\n\r\n";
                    output += TeamPrinter.Print(team);
                    output += "\r\n\r\n\r\n";
                }
            }

            return output;
        }
    }
}
