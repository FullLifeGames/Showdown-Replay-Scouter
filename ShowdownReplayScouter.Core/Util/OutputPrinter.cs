using ShowdownReplayScouter.Core.Data;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.Util
{
    public static class OutputPrinter
    {
        public static string Print(ScoutingRequest scoutingRequest, IEnumerable<Team> teams)
        {
            var output = scoutingRequest.User ?? "";
            output += scoutingRequest.Tier != null ? $" ({scoutingRequest.Tier})" : "";
            output += ":\r\n\r\n";

            foreach (var team in teams)
            {
                output += $"{team}:\r\n";
                output += string.Join("\r\n", team.Links);
                output += "\r\n\r\n";
                output += TeamPrinter.Print(team);
                output += "\r\n\r\n\r\n";
            }

            return output;
        }
    }
}
