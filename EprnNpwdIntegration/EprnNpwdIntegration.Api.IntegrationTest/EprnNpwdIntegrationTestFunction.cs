using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprnNpwdIntegration.Api.IntegrationTest
{
    public class EprnNpwdIntegrationTestFunction
    {
        private readonly ILogger<EprnNpwdIntegrationTestFunction> _logger;

        public EprnNpwdIntegrationTestFunction(ILogger<EprnNpwdIntegrationTestFunction> logger)
        {
            _logger = logger;
        }

        [Function("EprnNpwdIntegrationTestFunction")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("Invoking NPWD Test Function");
            return new OkObjectResult("NPWD Test");
        }
    }
}
