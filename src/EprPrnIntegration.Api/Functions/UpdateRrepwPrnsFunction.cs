using System.Net.Http.Json;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Api.Functions;

public class UpdateRrepwPrnsFunction(
    ILastUpdateService lastUpdateService,
    IPrnService prnService,
    IRrepwService rrepwService,
    ILogger<UpdateRrepwPrnsFunction> logger,
    IOptions<UpdateRrepwPrnsConfiguration> config
)
{
    [Function(FunctionName.UpdateRrepwPrns)]
    public async Task Run([TimerTrigger($"%{FunctionName.UpdateRrepwPrns}:Trigger%")] TimerInfo _)
    {
        List<PrnUpdateStatus>? updatedEprPrns = null;
        try
        {
            logger.LogInformation(
                "{FunctionId} function executed at: {DateTimeNow}",
                FunctionName.UpdateRrepwPrns,
                DateTime.UtcNow
            );

            var toDate = DateTime.UtcNow;
            var fromDate = await GetLastUpdate();

            // Retrieve data from the common backend
            updatedEprPrns = await GetUpdatedRrepwPrnsAsync(fromDate, toDate);
            if (updatedEprPrns == null)
                return;

            await UpdatePrns(updatedEprPrns, fromDate, toDate);

            await lastUpdateService.SetLastUpdate(FunctionName.UpdateRrepwPrns, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to update RrepwUpdatedPrns for {RrepwUpdatedPrns}",
                string.Join(",", updatedEprPrns?.Select(u => u.PrnNumber) ?? [])
            );
        }
    }

    /// <summary>
    ///  Send data to RREPW via pEPR API
    /// </summary>
    private async Task UpdatePrns(
        List<PrnUpdateStatus> updatedEprPrns,
        DateTime fromDate,
        DateTime toDate
    )
    {
        logger.LogInformation(
            "Sending total of {PrnCount} prns to RREPW for updating",
            updatedEprPrns.Count
        );

        await rrepwService.UpdatePrns(updatedEprPrns);
        logger.LogInformation(
            "Prns list successfully updated in RREPW for time period {FromDate} to {ToDate} limited to {RecordLimit}.",
            fromDate,
            toDate,
            config.Value.UpdateRrepwPrnsMaxRows
        );
    }

    private async Task<DateTime> GetLastUpdate()
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate(FunctionName.UpdateRrepwPrns);
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

    /// <summary>
    /// Retrieve data from the common backend
    /// </summary>
    private async Task<List<PrnUpdateStatus>?> GetUpdatedRrepwPrnsAsync(
        DateTime fromDate,
        DateTime toDate
    )
    {
        try
        {
            logger.LogInformation("Fetching Prns from {FromDate} to {ToDate}.", fromDate, toDate);
            var response = await prnService.GetUpdatedPrns(fromDate, toDate);
            response.EnsureSuccessStatusCode();

            var updatedEprPrns = await response.Content.ReadFromJsonAsync<List<PrnUpdateStatus>>();
            if (updatedEprPrns != null && updatedEprPrns.Count > 0)
                return updatedEprPrns;

            logger.LogWarning(
                "No updated Prns are retrieved from common database form time period {FromDate} to {ToDate}.",
                fromDate,
                toDate
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve data from common backend. form time period {FromDate} to {ToDate}.",
                fromDate,
                toDate
            );
        }

        return null;
    }
}
