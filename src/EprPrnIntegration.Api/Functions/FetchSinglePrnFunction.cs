using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace EprPrnIntegration.Api.Functions;

public class FetchSinglePrnFunction(IServiceBusProvider serviceBusProvider, INpwdClient npwdClient)
{

    [FunctionName("AddMissingPRNFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "prn")] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Processing request to add missing PRN.");

        try
        {
            var addMissingPrnRequest = await JsonSerializer.DeserializeAsync<AddMissingPrnRequest>(req.Body);

            if (addMissingPrnRequest?.PrnNumber is null)
            {
                return new BadRequestObjectResult("Please provide a valid PRN number.");
            }

            var fetchedMissingPrn = await FetchEvidenceFromNpwd(addMissingPrnRequest.PrnNumber, log);
            if (fetchedMissingPrn is null)
            {
                return new NotFoundObjectResult($"{addMissingPrnRequest.PrnNumber} is not found in NPWD system.");
            }

            await serviceBusProvider.SendFetchedNpwdPrnsToQueue([fetchedMissingPrn]);

            return new OkObjectResult($"{addMissingPrnRequest.PrnNumber} is produced to the queue to be processed.");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error processing PRN request.");
            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }
    }

    private async Task<NpwdPrn?> FetchEvidenceFromNpwd(string prnNumber, ILogger log)
    {
        var filter = $"(EvidenceStatusCode eq 'EV-CANCEL' or EvidenceStatusCode eq 'EV-AWACCEP' or EvidenceStatusCode eq 'EV-AWACCEP-EPR') AND evidenceNo eq '{prnNumber}'";

        log.LogInformation("Fetching evidence from NPWD API with filter: {filter}", filter);

        var issuedPrns = await npwdClient.GetIssuedPrns(filter);

        return issuedPrns is { Count: > 0 } ? issuedPrns.FirstOrDefault() : null;
    }
}