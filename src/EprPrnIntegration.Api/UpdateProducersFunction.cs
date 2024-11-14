using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace EprPrnIntegration.Api;

public class UpdateProducersFunction(IOrganisationService organisationService, TelemetryClient telemetryClient, 
    IConfigurationService configurationService, IHttpClientFactory httpClientFactory)
{
    
    [FunctionName("UpdateProducersList")]
    public async Task Run(
        [TimerTrigger("0 0 18 * * 1-5")] TimerInfo myTimer,
        ILogger log)
    {
        log.LogInformation($"UpdateProducersList function executed at: {DateTime.UtcNow}");

        try
        {
            // Retrieve data from the accounts backend
            var updatedProducers = new List<UpdatedProducersResponseModel>();
            var fromDate = DateTime.Today.AddDays(-1).AddHours(18); 
            var toDate = DateTime.Today.AddHours(18);               

            try
            {
                updatedProducers = await organisationService.GetUpdatedProducers(fromDate, toDate, new CancellationToken());
                if (updatedProducers == null)
                {
                    log.LogWarning($"No updated producers is retrieved from account database form time period {fromDate} to {toDate}.");
                    telemetryClient.TrackEvent("AccountsBackendDataFetchNoData");
                    return;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to retrieve data from accounts backend. form time period {fromDate} to {toDate}.");
                telemetryClient.TrackEvent("AccountsBackendDataFetchFailed");
                return;
            }

            // Send data to NPWD via pEPR API
            var producersData = JsonConvert.SerializeObject(updatedProducers);
            var requestContent = new StringContent(producersData, Encoding.UTF8, "application/json");

            var httpClient = httpClientFactory.CreateClient(Common.Constants.HttpClientNames.Npwd);

            var baseAddress = configurationService.GetNpwdApiBaseUrl();
            httpClient.BaseAddress = new Uri(baseAddress!);


            var pEprApiResponse = await httpClient.PostAsync("https://npwd-pepr-api/api/update-producers", 
                requestContent);

            if (pEprApiResponse.IsSuccessStatusCode)
            {
                log.LogInformation($"Producers list successfully updated in NPWD for time period {fromDate} to {toDate}.");
                telemetryClient.TrackEvent("ProducersListUpdateSuccess");
            }
            else
            {
                log.LogError($"Failed to update producers list in NPWD. Status Code: {pEprApiResponse.StatusCode}");
                telemetryClient.TrackEvent("ProducersListUpdateFailed");
            }
        }
        catch (Exception ex)
        {
            log.LogError($"An error occurred while updating the producers list: {ex.Message}");
            telemetryClient.TrackException(ex);
        }
    }
}