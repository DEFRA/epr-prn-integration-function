using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace EprPrnIntegration.Api.Functions;

public class FetchSinglePrnFunction(
    IServiceBusProvider serviceBusProvider, 
    INpwdClient npwdClient,
    ILogger<FetchSinglePrnFunction> logger)
{

    [Function("FetchSinglePrnFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "prn")] HttpRequest req)
    {
        logger.LogInformation("Processing request to add missing PRN.");

        try
        {
            var fetchSinglePrnRequest = await JsonSerializer.DeserializeAsync<FetchSinglePrnRequest>(req.Body);

            if (fetchSinglePrnRequest?.PrnNumber is null)
            {
                return new BadRequestObjectResult("Please provide a valid PRN number.");
            }

            var fetchedMissingPrn = await FetchEvidenceFromNpwd(fetchSinglePrnRequest.PrnNumber);
            if (fetchedMissingPrn is null)
            {
                return new NotFoundObjectResult($"{fetchSinglePrnRequest.PrnNumber} is not found in NPWD system.");
            }

            await serviceBusProvider.SendFetchedNpwdPrnsToQueue([fetchedMissingPrn]);

            return new OkObjectResult($"{fetchSinglePrnRequest.PrnNumber} is produced to the queue to be processed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PRN request.");
            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }
    }

    private async Task<NpwdPrn?> FetchEvidenceFromNpwd(string prnNumber)
    {
        var filter = $"(EvidenceStatusCode eq 'EV-CANCEL' or EvidenceStatusCode eq 'EV-AWACCEP' or EvidenceStatusCode eq 'EV-AWACCEP-EPR') AND evidenceNo eq '{prnNumber}'";

        logger.LogInformation("Fetching evidence from NPWD API with filter: {filter}", filter);

        var issuedPrns = await npwdClient.GetIssuedPrns(filter);

        return issuedPrns != null && issuedPrns?.Count > 0 ? issuedPrns.FirstOrDefault() : null;
    }
}