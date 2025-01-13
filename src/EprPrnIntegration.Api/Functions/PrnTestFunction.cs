using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace EprPrnIntegration.Api.Functions
{
    public class PrnTestFunction
    {
        private readonly IPrnService _prnService;

        public PrnTestFunction(IPrnService prnService)
        {
            _prnService = prnService;
        }

        [Function("PrnTestFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            try
            {
                var res =  await _prnService.GetUpdatedPrns(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), new CancellationToken());
                return new OkObjectResult(res);
            }
            catch (Exception ex)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}