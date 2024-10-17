using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp1
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
            try
            {
                return new StatusCodeResult(StatusCodes.Status200OK);
            }
            catch(Exception ex)
            {
                _logger.LogError(message: ex.Message, exception: ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
