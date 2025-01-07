using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions
{
    public class GetPersonsEmailsFunction
    {
        private readonly ILogger<GetPersonsEmailsFunction> _logger;
        private readonly IOrganisationService _organisationService;

        public GetPersonsEmailsFunction(ILogger<GetPersonsEmailsFunction> logger, IOrganisationService organisationService)
        {
            _logger = logger;
            _organisationService = organisationService;
        }

        [Function("GetPersonsEmailsFunction")]
        public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("HTTP trigger function Get Person Emails processed a request.");

            var organisationId = req.Query["organisationId"];
            var entityTypeCode = req.Query["entityTypeCode"];
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                _logger.LogWarning("Organisation ID is missing from the request.");
                return new BadRequestObjectResult("Organisation ID is required.");
            }

            try
            {
                var responseBody = await _organisationService.GetPersonEmailsAsync(organisationId!, entityTypeCode!, CancellationToken.None);
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
