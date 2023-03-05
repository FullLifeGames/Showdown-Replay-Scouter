using ShowdownReplayScouter.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.Util
{
    public class TeamComparer : IComparer<Team>
    {
        public int Compare(Team? x, Team? y)
        {
            var smogTourX = x?.Replays.Any((uri) => uri.Link.AbsoluteUri.Contains("smogtour")) == true;
            var smogTourY = y?.Replays.Any((uri) => uri.Link.AbsoluteUri.Contains("smogtour")) == true;
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
                var xMax = GetHighestNumber(x!.Replays.Where((uri) => uri.Link.AbsoluteUri.Contains("smogtour")));
                var yMax = GetHighestNumber(y!.Replays.Where((uri) => uri.Link.AbsoluteUri.Contains("smogtour")));
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
                var xMax = GetHighestNumber(x?.Replays);
                var yMax = GetHighestNumber(y?.Replays);
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

        private static int GetHighestNumber(IEnumerable<Replay>? replays)
        {
            if (replays?.Any() != true)
            {
                return -1;
            }

            var numberList = new List<int>();
            foreach (var replay in replays)
            {
                var currentUri = replay.Link.AbsoluteUri[(replay.Link.AbsoluteUri.LastIndexOf("/") + 1)..];
                var number = -1;
                while (currentUri.Contains('-'))
                {
                    var testNumber = currentUri[(currentUri.LastIndexOf("-") + 1)..];
                    currentUri = currentUri[..currentUri.LastIndexOf("-")];
                    if (int.TryParse(testNumber, out number))
                    {
                        break;
                    }
                }
                numberList.Add(number);
            }
            return numberList.Max();
        }
    }
}
