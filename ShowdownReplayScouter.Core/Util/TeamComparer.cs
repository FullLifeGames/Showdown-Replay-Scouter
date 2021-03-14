using ShowdownReplayScouter.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.Util
{
    public class TeamComparer : IComparer<Team>
    {
        public int Compare(Team x, Team y)
        {
            var smogTourX = x.Links.Any((uri) => uri.AbsoluteUri.Contains("smogtour"));
            var smogTourY = y.Links.Any((uri) => uri.AbsoluteUri.Contains("smogtour"));
            if (smogTourX && !smogTourY)
            {
                return -1;
            }
            if (smogTourY && !smogTourX)
            {
                return 1;
            }
            if (smogTourX && smogTourY)
            {
                var xMax = GetHighestNumber(x.Links.Where((uri) => uri.AbsoluteUri.Contains("smogtour")));
                var yMax = GetHighestNumber(y.Links.Where((uri) => uri.AbsoluteUri.Contains("smogtour")));
                if (xMax > yMax)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                var xMax = GetHighestNumber(x.Links);
                var yMax = GetHighestNumber(y.Links);
                if (xMax > yMax)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        private static int GetHighestNumber(IEnumerable<Uri> uris)
        {
            return uris.Select((uri) =>
                    int.Parse(uri.AbsoluteUri[(uri.AbsoluteUri.LastIndexOf("-") + 1)..]))
                        .Max();
        }
    }
}
