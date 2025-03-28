using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace EprPrnIntegration.Api.Functions;

public class UpdateProducersFunction(
    ICommonDataService commonDataService,
    INpwdClient npwdClient,
    ILogger<UpdateProducersFunction> logger,
    IConfiguration configuration,
    IUtilities utilities,
    IOptions<FeatureManagementConfiguration> featureConfig,
    IEmailService emailService)
{
    [Function("UpdateProducersList")]
    public async Task Run([TimerTrigger("%UpdateProducersTrigger%")] TimerInfo myTimer)
    {
        var isOn = featureConfig.Value.RunUpdateProducers ?? false;
        if (!isOn)
        {
            logger.LogInformation("UpdateProducersList function is disabled by feature flag");
            return;
        }

        logger.LogInformation("UpdateProducersList function executed at: {ExecutionDateTime}", DateTime.UtcNow);

        var deltaRun = await utilities.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers);

        var toDate = DateTime.UtcNow;
        var fromDate = deltaRun.LastSyncDateTime;

        logger.LogInformation("Fetching producers from {FromDate} to {ToDate}.", fromDate, toDate);

        var updatedEprProducers = await FetchUpdatedProducers(fromDate, toDate);
        if (updatedEprProducers == null || updatedEprProducers.Count.Equals(0))
        {
            logger.LogWarning("No updated producers retrieved for time period {FromDate} to {ToDate}.", fromDate, toDate);
            await utilities.SetDeltaSyncExecution(deltaRun, toDate);
            return;
        }

        var npwdUpdatedProducers = ProducerMapper.Map(updatedEprProducers, configuration);

        try
        {
            var wasSuccessful = await SendProducersInBatchesAsync(npwdUpdatedProducers.Value, fromDate, toDate);

            if (wasSuccessful)
            {
                await utilities.SetDeltaSyncExecution(deltaRun, toDate);
                LogCustomEvents(npwdUpdatedProducers.Value);
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
                    { CustomEventFields.OrganisationAddress, ProducerMapper.MapAddress(producer)},
                    { CustomEventFields.OrganisationType, producer.EntityTypeCode },
                    { CustomEventFields.OrganisationStatus, producer.StatusCode },
                    { CustomEventFields.OrganisationEprId, producer.EPRId },
                    { CustomEventFields.OrganisationRegNo, producer.CompanyRegNo }
                };

            utilities.AddCustomEvent(CustomEvents.UpdateProducer, eventData);
        }
    }

    private async Task<List<UpdatedProducersResponse>> FetchUpdatedProducers(DateTime fromDate, DateTime toDate)
    {
        try
        {
            return await commonDataService.GetUpdatedProducers(fromDate, toDate, new CancellationToken());
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve data from accounts backend for time period {FromDate} to {ToDate}.", fromDate, toDate);
            throw;
        }
    }

    private async Task<bool> SendProducersInBatchesAsync(List<Producer> producers, DateTime fromDate, DateTime toDate)
    {
        var batchSize = int.TryParse(configuration["UpdateProducersBatchSize"], out var batch) ? batch : 100;
    
        var totalCount = producers.Count;

        for (var i = 0; i < totalCount; i += batchSize)
        {
            var producerBatch = producers.Skip(i).Take(batchSize).ToList();

            try
            {
                var batchResponse = await npwdClient.Patch(producerBatch, NpwdApiPath.Producers);

                if (batchResponse.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "Batch {BatchStart}-{BatchEnd} successfully updated in NPWD for time period {FromDate} to {ToDate}.",
                        i + 1, Math.Min(i + batchSize, totalCount), fromDate, toDate);
                }
                else
                {
                    var responseBody = await batchResponse.Content.ReadAsStringAsync();
                    logger.LogError(
                        "Failed to update producer batch {BatchStart}-{BatchEnd}. Status: {StatusCode}, Body: {ResponseBody}",
                        i + 1, Math.Min(i + batchSize, totalCount), batchResponse.StatusCode, responseBody);

                    if (batchResponse.StatusCode >= HttpStatusCode.InternalServerError || batchResponse.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        emailService.SendErrorEmailToNpwd(
                            $"Failed to update producer batch {i + 1}-{Math.Min(i + batchSize, totalCount)}. " +
                            $"Status: {batchResponse.StatusCode}, Body: {responseBody}");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Exception while sending producer batch {BatchStart}-{BatchEnd} to NPWD.",
                    i + 1, Math.Min(i + batchSize, totalCount));
                return false;
            }
        }

        return true;
    }
}