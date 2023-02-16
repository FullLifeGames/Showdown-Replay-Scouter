using ShowdownReplayScouter.Core.Data;

namespace ShowdownReplayScouter.Api.Data
{
    [Serializable]
    public class ApiScoutingRequest : ScoutingRequest
    {
        public bool? ProvideOutput { get; set; }
    }
}
