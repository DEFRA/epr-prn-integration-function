using System.Net;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwPrnService.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

public class UpdateRrepwPrnsFunction(
    ILogger<UpdateRrepwPrnsFunction> logger,
    IRrepwPrnService rrepwPrnService,
    IPrnService prnService,
    IConfiguration configuration)
{
    [Function("UpdateRrepwPrns")]
    public async Task Run([TimerTrigger("%UpdateRrepwPrns:Trigger%")] TimerInfo myTimer)
    {
        logger.LogInformation("UpdateRrepwPrns function started at: {ExecutionDateTime}", DateTime.UtcNow);

        var prns = await rrepwPrnService.GetPrns(CancellationToken.None);

        if (!prns.Any())
        {
            logger.LogInformation("No PRNs found from RREPW service; terminating.");
            return;
        }

        logger.LogInformation("Found {Count} PRN(s) to process", prns.Count);

        await ProcessPrns(prns);

        logger.LogInformation("UpdateRrepwPrns function completed at: {ExecutionDateTime}", DateTime.UtcNow);
    }

    private async Task ProcessPrns(List<NpwdPrn> prns)
    {
        await Parallel.ForEachAsync(prns, new ParallelOptions
        {
            MaxDegreeOfParallelism = 20
        }, async (prn, _) => await ProcessPrn(prn));
    }

    private async Task ProcessPrn(NpwdPrn prn)
    {
        try
        {
            var request = NpwdPrnToSavePrnDetailsRequestMapper.Map(prn, configuration, logger);
            await prnService.SavePrn(request);
            logger.LogInformation("Successfully saved PRN {EvidenceNo}", prn.EvidenceNo);
        }
        catch (HttpRequestException ex) when (IsTransient(ex))
        {
            // Allow the function to terminate and resume on the next schedule.
            logger.LogError(ex, "Service unavailable ({StatusCode}) when saving PRN {EvidenceNo}, rethrowing", ex.StatusCode, prn.EvidenceNo);
            throw;
        }
        catch (Exception ex)
        {
            // We want to swallow non-transient errors since they'll never be recoverable; all we can do is log errors
            // to allow investigation.
            logger.LogError(ex, "Failed to save PRN {EvidenceNo}, continuing with next PRN", prn.EvidenceNo);
        }
    }

    // 5xx, 408 or 429.
    private static bool IsTransient(HttpRequestException ex)
    {
        return ex.StatusCode is >= HttpStatusCode.InternalServerError or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;
    }
}
