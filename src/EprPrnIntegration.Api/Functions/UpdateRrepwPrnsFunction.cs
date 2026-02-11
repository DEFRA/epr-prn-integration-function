using System.Net;
using System.Net.Http.Json;
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
    IOptions<UpdateRrepwPrnsConfiguration> config,
    IUtilities utilities
)
{
    [Function(FunctionName.UpdateRrepwPrns)]
    public async Task Run([TimerTrigger($"%{FunctionName.UpdateRrepwPrns}:Trigger%")] TimerInfo _)
    {
        logger.LogInformation(
            "{FunctionId} function executed at: {DateTimeNow}",
            FunctionName.UpdateRrepwPrns,
            DateTime.UtcNow
        );

        var toDate = DateTime.UtcNow;
        var fromDate = await GetLastUpdate();

        // Retrieve data from the common backend
        List<PrnUpdateStatus>? updatedEprPrns = await GetUpdatedRrepwPrnsAsync(fromDate, toDate);
        if (updatedEprPrns == null)
            return;

        foreach (var prn in updatedEprPrns)
        {
            await UpdatePrn(prn, fromDate, toDate);
            LogCustomEvents(prn);
        }

        await lastUpdateService.SetLastUpdate(FunctionName.UpdateRrepwPrns, DateTime.UtcNow);
    }

    /// <summary>
    ///  Send data to RREPW via pEPR API
    /// </summary>
    private async Task UpdatePrn(PrnUpdateStatus prn, DateTime fromDate, DateTime toDate)
    {
        await HttpHelper.HandleTransientErrors(
            async (ct) => await rrepwService.UpdatePrn(prn),
            logger,
            $"Updating Prn {prn.PrnNumber} in RREPW for time period {fromDate} to {toDate}.",
            shouldNotContinueOn:
            [
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NotFound,
            ],
            CancellationToken.None
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
        logger.LogInformation("Fetching Prns from {FromDate} to {ToDate}.", fromDate, toDate);
        var response = await prnService.GetUpdatedPrns(fromDate, toDate);
        response.EnsureSuccessStatusCode();

        var updatedEprPrns = await response.Content.ReadFromJsonAsync<List<PrnUpdateStatus>>();
        if (updatedEprPrns != null && updatedEprPrns.Count > 0)
            return updatedEprPrns;

        logger.LogInformation(
            "No updated Prns are retrieved from common database form time period {FromDate} to {ToDate}.",
            fromDate,
            toDate
        );
        return null;
    }

    private void LogCustomEvents(PrnUpdateStatus prn)
    {
        Dictionary<string, string> eventData = new()
        {
            { "EvidenceNo", prn.PrnNumber },
            { "EvidenceStatusCode", prn.PrnStatusId.ToString() },
            { "StatusDate", prn.StatusDate.GetValueOrDefault().ToUniversalTime().ToString() },
        };

        utilities.AddCustomEvent(CustomEvents.UpdatePrnRrepw, eventData);
    }
}
