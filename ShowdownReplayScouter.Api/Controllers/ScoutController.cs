using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using ShowdownReplayScouter.Api.Data;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.ReplayScouter;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("fixed")]
    public class ScoutController : ControllerBase
    {
        /// <summary>
        /// The reverse proxy gives up after 20 minutes (Apache "ProxyTimeout 1200"), so stop a
        /// little earlier: the client gets a clear answer and no work keeps running for nobody.
        /// </summary>
        private static readonly TimeSpan MaximumScoutingDuration = TimeSpan.FromMinutes(19);

        private readonly ReplayScouter _replayScouter;
        private readonly ILogger<ScoutController> _logger;

        public ScoutController(ILogger<ScoutController> logger)
        {
            _replayScouter = new Core.ReplayScouter.ShowdownReplayScouter();
            _logger = logger;
        }

        [OutputCache(VaryByQueryKeys = ["*"], Duration = 30)]
        [HttpGet(Name = "GetScoutingResult")]
        public async Task<ActionResult<ApiScoutingResult?>> Get(
            [FromQuery] ApiScoutingRequest scoutingRequest
        )
        {
            return await RequestHandler(scoutingRequest).ConfigureAwait(false);
        }

        [OutputCache(Duration = 60, PolicyName = "post")]
        [HttpPost(Name = "PostScoutingResult")]
        public async Task<ActionResult<ApiScoutingResult?>> Post(
            [FromBody] ApiScoutingRequest scoutingRequest
        )
        {
            return await RequestHandler(scoutingRequest).ConfigureAwait(false);
        }

        private async Task<ActionResult<ApiScoutingResult?>> RequestHandler(
            ApiScoutingRequest scoutingRequest
        )
        {
            // Stops downloading and analyzing replays once the client is gone or the time is up
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                HttpContext.RequestAborted
            );
            cancellation.CancelAfter(MaximumScoutingDuration);

            ScoutingResult? basicScoutingResult;
            try
            {
                basicScoutingResult = await _replayScouter
                    .ScoutReplaysAsync(scoutingRequest, cancellation.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                if (HttpContext.RequestAborted.IsCancellationRequested)
                {
                    _logger.LogInformation("Scouting canceled, the client disconnected");
                    return new EmptyResult();
                }
                _logger.LogWarning(
                    "Scouting canceled after {Minutes} minutes",
                    MaximumScoutingDuration.TotalMinutes
                );
                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    $"Scouting took longer than {MaximumScoutingDuration.TotalMinutes} minutes, "
                        + "please narrow the request (e.g. with a tier or a date range)."
                );
            }

            ApiScoutingResult? scoutingResult = null;
            if (basicScoutingResult is not null)
            {
                scoutingResult = new ApiScoutingResult(basicScoutingResult);
                if (scoutingRequest.ProvideOutput == true)
                {
                    scoutingResult.Outputs = OutputPrinter.PrintObject(
                        scoutingRequest,
                        scoutingResult.Teams
                    );
                }
            }
            return scoutingResult;
        }
    }
}
