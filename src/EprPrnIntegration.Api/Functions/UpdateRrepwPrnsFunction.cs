using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
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
    public const string FunctionId = "UpdateRrepwPrnsList";

    [Function(FunctionId)]
    public async Task Run([TimerTrigger("%UpdateRrepwPrnsTrigger%")] TimerInfo _)
    {
        List<PrnUpdateStatus>? updatedEprPrns = null;
        try
        {
            logger.LogInformation(
                "UpdateRrepwPrnsList function executed at: {DateTimeNow}",
                DateTime.UtcNow
            );

            var toDate = DateTime.UtcNow;
            var fromDate = await GetLastUpdate();

            // Retrieve data from the common backend
            updatedEprPrns = await GetUpdatedRrepwPrnsAsync(fromDate, toDate);
            if (updatedEprPrns == null)
                return;

            updatedEprPrns = LimitRecords(updatedEprPrns);

            await UpdatePrns(updatedEprPrns, fromDate, toDate);

            await lastUpdateService.SetLastUpdate(FunctionId, DateTime.UtcNow);

            // todo do we need this LogCustomEvents(updatedEprPrns);
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

    /// <summary>
    /// Limit the number of records sent to RREPW
    /// </summary>
    private List<PrnUpdateStatus> LimitRecords(List<PrnUpdateStatus> updatedEprPrns)
    {
        // owing to performance limitations (timeouts) on external service, limit number of rows sent in a batch
        // todo do we need this?
        if (
            config.Value.UpdateRrepwPrnsMaxRows > 0
            && config.Value.UpdateRrepwPrnsMaxRows < updatedEprPrns.Count
        )
        {
            logger.LogInformation(
                "Batching {BatchSize} of {PrnCount} Prns",
                config.Value.UpdateRrepwPrnsMaxRows,
                updatedEprPrns.Count
            );

            updatedEprPrns =
            [
                .. updatedEprPrns
                    .OrderBy(x => x.StatusDate)
                    .Take(config.Value.UpdateRrepwPrnsMaxRows),
            ];
        }
        return updatedEprPrns;
    }

    private async Task<DateTime> GetLastUpdate()
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate(FunctionId);
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

    // do we need this? private void LogCustomEvents(IEnumerable<PrnUpdateStatus> rrepwUpdatedPrns)
    // {
    //     foreach (var prn in rrepwUpdatedPrns)
    //     {
    //         Dictionary<string, string> eventData = new()
    //         {
    //             { "PrnNumber", prn.PrnNumber },
    //             { "PrnStatusId", prn.PrnStatusId.ToString() },
    //             { "StatusDate", prn.StatusDate.GetValueOrDefault().ToUniversalTime().ToString() },
    //             { "SourceSystemId", prn.SourceSystemId.ToString() },
    //             { "AccreditationYear", prn.AccreditationYear.ToString() },
    //         };

    //         utilities.AddCustomEvent(CustomEvents.UpdatePrn, eventData);
    //     }
    // }

    // Retrieve data from the common backend
    private async Task<List<PrnUpdateStatus>?> GetUpdatedRrepwPrnsAsync(
        DateTime fromDate,
        DateTime toDate
    )
    {
        try
        {
            logger.LogInformation("Fetching Prns from {FromDate} to {ToDate}.", fromDate, toDate);
            var updatedEprPrns = await prnService.GetUpdatedPrns(fromDate, toDate);
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
