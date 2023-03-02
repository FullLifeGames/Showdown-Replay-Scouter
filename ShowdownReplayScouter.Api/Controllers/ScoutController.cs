using Microsoft.AspNetCore.Mvc;
using NeoSmart.Caching.Sqlite;
using ShowdownReplayScouter.Api.Data;
using ShowdownReplayScouter.Core.ReplayScouter;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScoutController : ControllerBase
    {
        private readonly ReplayScouter _replayScouter;

        public ScoutController(IWebHostEnvironment hostingEnv)
        {
            _replayScouter = new Core.ReplayScouter.ShowdownReplayScouter(
                new SqliteCache(
                    new SqliteCacheOptions()
                    {
                        MemoryOnly = false,
                        CachePath = hostingEnv.IsDevelopment() 
                            ? "ShowdownReplayScouter.db"
                            : "/home/apache/ShowdownReplayScouter.Cmd/ShowdownReplayScouter.db",
                    }
                )
            );
        }

        [HttpGet(Name = "GetScoutingResult")]
        public async Task<ApiScoutingResult?> Get([FromQuery] ApiScoutingRequest scoutingRequest)
        {
            return await RequestHandler(scoutingRequest).ConfigureAwait(false);
        }

        [HttpPost(Name = "PostScoutingResult")]
        public async Task<ApiScoutingResult?> Post([FromBody] ApiScoutingRequest scoutingRequest)
        {
            return await RequestHandler(scoutingRequest).ConfigureAwait(false);
        }

        private async Task<ApiScoutingResult?> RequestHandler(ApiScoutingRequest scoutingRequest)
        {
            var basicScoutingResult = await _replayScouter.ScoutReplaysAsync(scoutingRequest).ConfigureAwait(false);
            ApiScoutingResult? scoutingResult = null;
            if (basicScoutingResult is not null)
            {
                scoutingResult = new ApiScoutingResult(basicScoutingResult);
                if (scoutingRequest.ProvideOutput == true)
                {
                    scoutingResult.Outputs = OutputPrinter.PrintObject(scoutingRequest, scoutingResult.Teams);
                }
            }
            return scoutingResult;
        }
    }
}