using AutoMapper;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Api.Functions;

public class FetchRrepwIssuedPrnsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<FetchRrepwIssuedPrnsFunction> logger,
    IRrepwService rrepwService,
    IPrnService prnService,
    IOptions<FetchRrepwIssuedPrnsConfiguration> config
)
{
    private readonly IMapper _mapper = RrepwMappers.CreateMapper();

    [Function(FunctionName.FetchRrepwIssuedPrns)]
    public async Task Run(
        [TimerTrigger($"%{FunctionName.FetchRrepwIssuedPrns}:Trigger%")] TimerInfo myTimer
    )
    {
        var lastUpdate = await GetLastUpdate();
        logger.LogInformation(
            "{FunctionId} resuming with last update time: {ExecutionDateTime}",
            FunctionName.FetchRrepwIssuedPrns,
            lastUpdate
        );

        var utcNow = DateTime.UtcNow;

        var prns = await rrepwService.ListPackagingRecyclingNotes(lastUpdate, utcNow);

        if (!prns.Any())
        {
            logger.LogInformation("No PRNs found from RREPW service; terminating.");
            return;
        }

        logger.LogInformation("Found {Count} PRN(s) to process", prns.Count);

        await ProcessPrns(prns);

        await lastUpdateService.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, utcNow);
        logger.LogInformation(
            "{FunctionId} function completed at: {ExecutionDateTime}",
            FunctionName.FetchRrepwIssuedPrns,
            DateTime.UtcNow
        );
    }

    private async Task<DateTime> GetLastUpdate()
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
        if (!lastUpdate.HasValue)
        {
            return DateTime.SpecifyKind(
                DateTime.ParseExact(
                    config.Value.DefaultStartDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                DateTimeKind.Utc
            );
        }
        return lastUpdate.Value;
    }

    private async Task ProcessPrns(List<PackagingRecyclingNote> prns)
    {
        logger.LogInformation("Processing {Count} prns", prns.Count);
        foreach (var prn in prns)
        {
            await ProcessPrn(prn);
        }
    }

    private async Task ProcessPrn(PackagingRecyclingNote prn)
    {
        var request = _mapper.Map<SavePrnDetailsRequest>(prn);
        var response = await prnService.SavePrn(request);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successfully saved PRN {PrnNumber}", prn.PrnNumber);
            return;
        }

        // Transient errors after Polly retries exhausted - terminate function to retry on next schedule
        if (response.StatusCode.IsTransient())
        {
            logger.LogError(
                "Service unavailable ({StatusCode}) when saving PRN {PrnNumber}, terminating function",
                response.StatusCode,
                prn.PrnNumber
            );
            throw new HttpRequestException(
                $"Transient error {response.StatusCode} saving PRN {prn.PrnNumber}",
                null,
                response.StatusCode
            );
        }

        // Non-transient errors are not recoverable; log and continue with next PRN
        logger.LogError(
            "Failed to save PRN {PrnNumber} with status {StatusCode}, continuing with next PRN",
            prn.PrnNumber,
            response.StatusCode
        );
    }
}
