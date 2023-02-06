using Microsoft.AspNetCore.Mvc;
using NeoSmart.Caching.Sqlite;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayScouter;

namespace ShowdownReplayScouter.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScoutController : ControllerBase
    {
        private readonly ReplayScouter _replayScouter;

        public ScoutController()
        {
            _replayScouter = new Core.ReplayScouter.ShowdownReplayScouter(
                new SqliteCache(
                    new SqliteCacheOptions()
                    {
                        MemoryOnly = false,
                        CachePath = "ShowdownReplayScouter.db",
                    }
                )
            );
        }

        [HttpGet(Name = "GetScoutingResult")]
        public async Task<ScoutingResult?> Get([FromQuery] ScoutingRequest scoutingRequest)
        {
            return await _replayScouter.ScoutReplaysAsync(scoutingRequest);
        }
    }
}