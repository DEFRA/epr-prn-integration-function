using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api
{
    public class SendPersonsEmailsFunction
    {
        private readonly ILogger<SendPersonsEmailsFunction> _logger;
        private readonly IOrganisationService _organisationService;

        public SendPersonsEmailsFunction(ILogger<SendPersonsEmailsFunction> logger, IOrganisationService organisationService)
        {
            _logger = logger;
            _organisationService = organisationService;
        }

        [Function("SendPersonsEmailsFunction")]
        public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("HTTP trigger function Get Person Emails processed a request.");

            //string organisationId = req.Query["organisationId"];
            //if (string.IsNullOrWhiteSpace(organisationId))
            //{
            //    _logger.LogWarning("Organisation ID is missing from the request.");
            //    return new BadRequestObjectResult("Organisation ID is required.");
            //}

            var organisationId = "78C3D531-BB34-41DF-8693-C9A9382EB879";

            try
            {
                
                var responseBody = await _organisationService.GetPersonEmailsAsync(organisationId, CancellationToken.None);
                return new OkObjectResult(responseBody);
            }
            catch (HttpRequestException)
            {
                return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }

}
