using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api
{
    public class PrnFunction
    {
        private readonly ILogger<PrnFunction> _logger;

        public PrnFunction(ILogger<PrnFunction> logger)
        {
            _logger = logger;
        }

        [Function("PrnFunction")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("Invoking Get all PRNs NPWD API");
            try
            {
                return new OkObjectResult("NPWD Test");
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: ex.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
