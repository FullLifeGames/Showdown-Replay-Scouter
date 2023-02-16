using ShowdownReplayScouter.Core.Data;

namespace ShowdownReplayScouter.Api.Data
{
    [Serializable]
    public class ApiScoutingResult : ScoutingResult
    {
        public ApiScoutingResult()
        {
        }

        public ApiScoutingResult(ScoutingResult scoutingResult)
        {
            Teams = scoutingResult.Teams;
        }

        public OutputObject? Outputs { get; set; }
    }
}
