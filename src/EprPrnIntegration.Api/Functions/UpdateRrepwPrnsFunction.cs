using System.Net;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwPrnService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Api.Functions;

public class UpdateRrepwPrnsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<UpdateRrepwPrnsFunction> logger,
    IRrepwPrnService rrepwPrnService,
    IPrnService prnService,
    IConfiguration configuration,
    IOptions<UpdateRrepwPrnsConfiguration> config)
{
    [Function("UpdateRrepwPrns")]
    public async Task Run([TimerTrigger("%UpdateRrepwPrns:Trigger%")] TimerInfo myTimer)
    {
        var lastUpdate = await GetLastUpdate();
        logger.LogInformation("UpdateRrepwPrns resuming with last update time: {ExecutionDateTime}", lastUpdate);

        var utcNow = DateTime.UtcNow;

        var prns = await rrepwPrnService.GetPrns(CancellationToken.None);

        if (!prns.Any())
        {
            logger.LogInformation("No PRNs found from RREPW service; terminating.");
            return;
        }

        logger.LogInformation("Found {Count} PRN(s) to process", prns.Count);

        await ProcessPrns(prns);

        await lastUpdateService.SetLastUpdate("UpdateRrepwPrns", utcNow);
        logger.LogInformation("UpdateRrepwPrns function completed at: {ExecutionDateTime}", DateTime.UtcNow);
    }

    private async Task<DateTime> GetLastUpdate()
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate("UpdateRrepwPrns");
        if (!lastUpdate.HasValue)
        {
            return DateTime.SpecifyKind(
                DateTime.ParseExact(config.Value.DefaultStartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                DateTimeKind.Utc
            );
        }
        return lastUpdate!.Value;
    }

    private async Task ProcessPrns(List<PackagingRecyclingNote> prns)
    {
        await Parallel.ForEachAsync(prns, new ParallelOptions
        {
            MaxDegreeOfParallelism = 20
        }, async (prn, _) => await ProcessPrn(prn));
    }

    private async Task ProcessPrn(PackagingRecyclingNote prn)
    {
        try
        {
            var request = PackagingRecyclingNoteToSavePrnDetailsRequestMapper.Map(prn, configuration, logger);
            await prnService.SavePrn(request);
            logger.LogInformation("Successfully saved PRN {PrnNumber}", prn.PrnNumber);
        }
        catch (HttpRequestException ex) when (ex.IsTransient())
        {
            // Allow the function to terminate and resume on the next schedule.
            logger.LogError(ex, "Service unavailable ({StatusCode}) when saving PRN {PrnNumber}, rethrowing", ex.StatusCode, prn.PrnNumber);
            throw;
        }
        catch (Exception ex)
        {
            // We want to swallow non-transient errors since they'll never be recoverable; all we can do is log errors
            // to allow investigation.
            logger.LogError(ex, "Failed to save PRN {PrnNumber}, continuing with next PRN", prn.PrnNumber);
        }
    }
}
