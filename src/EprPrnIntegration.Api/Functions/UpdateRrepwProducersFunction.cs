using System.Net;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

public class UpdateRrepwProducersFunction(
    ICommonDataService commonDataService,
    INpwdClient npwdClient,
    ILogger<UpdateRrepwProducersFunction> logger,
    IConfiguration configuration,
    IUtilities utilities)
{
    [Function("UpdateRrepwProducersList")]
    public async Task Run([TimerTrigger("%UpdateRrepwProducersTrigger%")] TimerInfo myTimer)
    {
        logger.LogInformation("UpdateRrepwProducersList function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        var deltaRun = await utilities.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedRrepwProducers);

        var toDate = DateTime.UtcNow;
        var fromDate = deltaRun.LastSyncDateTime;

        logger.LogInformation("Fetching producers from {FromDate} to {ToDate}.", fromDate, toDate);

        var updatedEprProducers = await FetchUpdatedProducers(fromDate, toDate);
        if (updatedEprProducers == null || updatedEprProducers.Count.Equals(0))
        {
            logger.LogWarning("No updated producers retrieved for time period {FromDate} to {ToDate}.", fromDate,
                toDate);
            return;
        }

        updatedEprProducers = updatedEprProducers.OrderBy(x => x.UpdatedDateTime).ToList();

        if (int.TryParse(configuration["UpdateRrepwProducersBatchSize"], out var batchSize))
            if (batchSize > 0 && batchSize < updatedEprProducers.Count)
            {
                logger.LogInformation("Batching {BatchSize} of {ProducersCount} producers", batchSize,
                    updatedEprProducers.Count);
                updatedEprProducers = updatedEprProducers.Take(batchSize).ToList();
            }

        var newestProducerStatusDate = updatedEprProducers.Select(x => x.UpdatedDateTime).LastOrDefault();
        if (newestProducerStatusDate.GetValueOrDefault() > DateTime.MinValue)
            toDate = newestProducerStatusDate.GetValueOrDefault().ToUniversalTime();

        var npwdUpdatedProducers = ProducerMapper.Map(updatedEprProducers, configuration);

        try
        {
            logger.LogInformation("Sending total of {ProducerCount} producer to npwd for updating",
                updatedEprProducers.Count);

            var pEprApiResponse = await npwdClient.Patch(npwdUpdatedProducers, NpwdApiPath.Producers);

            if (pEprApiResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Producers list successfully updated in NPWD for time period {FromDate} to {ToDate}.", fromDate,
                    toDate);

                await utilities.SetDeltaSyncExecution(deltaRun, toDate);

                LogCustomEvents(npwdUpdatedProducers.Value);
            }
            else
            {
                var responseBody = await pEprApiResponse.Content.ReadAsStringAsync();
                logger.LogError(
                    "Failed to update producer lists. error code {StatusCode} and raw response body: {ResponseBody}",
                    pEprApiResponse.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"An error was encountered on attempting to call NPWD API {NpwdApiPath.Producers}");
        }
    }

    private void LogCustomEvents(IEnumerable<Producer> updatedProducers)
    {
        foreach (var producer in updatedProducers)
        {
            Dictionary<string, string> eventData = new()
            {
                { CustomEventFields.OrganisationName, producer.ProducerName },
                { CustomEventFields.OrganisationId, producer.EPRCode },
                { CustomEventFields.Date, DateTime.UtcNow.ToString() },
                { CustomEventFields.OrganisationAddress, ProducerMapper.MapAddress(producer) },
                { CustomEventFields.OrganisationType, producer.EntityTypeCode },
                { CustomEventFields.OrganisationStatus, producer.StatusCode },
                { CustomEventFields.OrganisationEprId, producer.EPRId },
                { CustomEventFields.OrganisationRegNo, producer.CompanyRegNo }
            };

            utilities.AddCustomEvent(CustomEvents.UpdateRrepwProducers, eventData);
        }
    }

    private async Task<List<UpdatedProducersResponse>> FetchUpdatedProducers(DateTime fromDate, DateTime toDate)
    {
        try
        {
            return await commonDataService.GetUpdatedProducers(fromDate, toDate, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve data from accounts backend for time period {FromDate} to {ToDate}.", fromDate,
                toDate);
            throw;
        }
    }
}