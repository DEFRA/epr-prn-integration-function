using System.Net;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Api.Functions;

public class UpdateNpwdPrnsFunction(
    INpwdPrnService prnService,
    INpwdClient npwdClient,
    ILogger<UpdateNpwdPrnsFunction> logger,
    IConfiguration configuration,
    IOptions<FeatureManagementConfiguration> featureConfig,
    IUtilities utilities,
    IEmailService emailService
)
{
    [Function("UpdatePrnsList")]
    public async Task Run([TimerTrigger("%UpdatePrnsTrigger%")] TimerInfo myTimer)
    {
        bool isOn = featureConfig.Value.RunIntegration ?? false;
        if (!isOn)
        {
            logger.LogInformation("UpdatePrnsList function is disabled by feature flag");
            return;
        }

        logger.LogInformation(
            "UpdatePrnsList function executed at: {DateTimeNow}",
            DateTime.UtcNow
        );

        var deltaRun = await utilities.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns);

        var toDate = DateTime.UtcNow;
        var fromDate = deltaRun.LastSyncDateTime;

        logger.LogInformation("Fetching Prns from {FromDate} to {ToDate}.", fromDate, toDate);

        // Retrieve data from the common backend
        var updatedEprPrns = await GetUpdatedNpwdPrnsAsync(fromDate, toDate);
        if (updatedEprPrns == null)
            return;

        foreach (var prn in updatedEprPrns)
        {
            logger.LogDebug(
                "Updating PRN in NPWD: prn number (evidence no): {Prn} status code: {StatusCode} status date: {StatusDate} obligation year: {ObligationYear}",
                prn.EvidenceNo,
                prn.EvidenceStatusCode,
                prn.StatusDate,
                prn.ObligationYear
            );
        }

        // owing to performance limitations (timeouts) on external service, limit number of rows sent in a batch
        if (
            int.TryParse(configuration["UpdatePrnsMaxRows"], out int maxRows)
            && maxRows > 0
            && maxRows < updatedEprPrns.Count
        )
        {
            logger.LogInformation(
                "Batching {BatchSize} of {PrnCount} Prns",
                maxRows,
                updatedEprPrns.Count
            );

            updatedEprPrns = updatedEprPrns.OrderBy(x => x.StatusDate).Take(maxRows).ToList();

            var newestPrnStatusDate = updatedEprPrns.Select(x => x.StatusDate).LastOrDefault();
            if (newestPrnStatusDate.GetValueOrDefault() > DateTime.MinValue)
            {
                toDate = newestPrnStatusDate.GetValueOrDefault().ToUniversalTime();
            }
        }

        // Send data to NPWD via pEPR API
        var npwdUpdatedPrns = PrnMapper.Map(updatedEprPrns, configuration);

        try
        {
            logger.LogInformation(
                "Sending total of {PrnCount} prns to npwd for updating",
                updatedEprPrns.Count
            );

            var pEprApiResponse = await npwdClient.Patch(npwdUpdatedPrns, NpwdApiPath.Prns);

            if (pEprApiResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Prns list successfully updated in NPWD for time period {FromDate} to {DateTime}.",
                    fromDate,
                    toDate
                );

                await utilities.SetDeltaSyncExecution(deltaRun, toDate);

                // Insert sync data into common prn backend
                await prnService.InsertPeprNpwdSyncPrns(npwdUpdatedPrns.Value);

                LogCustomEvents(npwdUpdatedPrns.Value);
            }
            else
            {
                var responseBody = await pEprApiResponse.Content.ReadAsStringAsync();

                logger.LogError(
                    "Failed to update producer lists. error code {StatusCode} and raw response body: {ResponseBody}",
                    pEprApiResponse.StatusCode,
                    responseBody
                );

                logger.LogError(
                    "Failed to update Prns list in NPWD. Status Code: {StatusCode}",
                    pEprApiResponse.StatusCode
                );

                if (
                    pEprApiResponse.StatusCode >= HttpStatusCode.InternalServerError
                    || pEprApiResponse.StatusCode == HttpStatusCode.RequestTimeout
                )
                {
                    emailService.SendErrorEmailToNpwd(
                        $"Failed to update producer lists. error code {pEprApiResponse.StatusCode} and raw response body: {responseBody}"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to patch NpwdUpdatedPrns for {NpwdUpdatedPrns}",
                npwdUpdatedPrns.ToString()
            );
        }
    }

    private void LogCustomEvents(IEnumerable<UpdatedNpwdPrnsResponseModel> npwdUpdatedPrns)
    {
        foreach (var prn in npwdUpdatedPrns)
        {
            Dictionary<string, string> eventData = new()
            {
                { "EvidenceNo", prn.EvidenceNo },
                { "EvidenceStatusCode", prn.EvidenceStatusCode },
                { "StatusDate", prn.StatusDate.GetValueOrDefault().ToUniversalTime().ToString() },
            };

            utilities.AddCustomEvent(CustomEvents.UpdatePrn, eventData);
        }
    }

    // Retrieve data from the common backend
    private async Task<List<UpdatedNpwdPrnsResponseModel>?> GetUpdatedNpwdPrnsAsync(
        DateTime fromDate,
        DateTime toDate
    )
    {
        try
        {
            var updatedEprPrns = await prnService.GetUpdatedNpwdPrns(
                fromDate,
                toDate,
                CancellationToken.None
            );
            if (updatedEprPrns != null && updatedEprPrns.Count > 0)
            {
                return updatedEprPrns;
            }

            logger.LogWarning(
                "No updated Prns are retrieved for common database for time period {FromDate} to {ToDate}.",
                fromDate,
                toDate
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve data for common backend for time period {FromDate} to {ToDate}.",
                fromDate,
                toDate
            );
        }

        return null;
    }
}
