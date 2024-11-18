using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.NpwdService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api;

public class UpdateProducersFunction(IOrganisationService organisationService, IProducerService producerService, 
    ILogger<UpdateProducersFunction> logger)
{
    [FunctionName("UpdateProducersList")]
    public async Task Run(
        [TimerTrigger("0 0 18 * * 1-5")] TimerInfo myTimer)
    {
        logger.LogInformation($"UpdateProducersList function executed at: {DateTime.UtcNow}");

        try
        {
            // Retrieve data from the accounts backend
            var updatedEprProducers = new List<UpdatedProducersResponseModel>();
            var fromDate = DateTime.Today.AddDays(-1).AddHours(18); 
            var toDate = DateTime.Today.AddHours(18);               

            try
            {
                updatedEprProducers = await organisationService.GetUpdatedProducers(fromDate, toDate, new CancellationToken());
                if (updatedEprProducers == null || !updatedEprProducers.Any())
                {
                    logger.LogWarning($"No updated producers is retrieved from account database form time period {fromDate} to {toDate}.");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to retrieve data from accounts backend. form time period {fromDate} to {toDate}.");
                return;
            }

            // Send data to NPWD via pEPR API
            var npwdUpdatedProducers = ProducerMapper.Map(updatedEprProducers);

            var pEprApiResponse = await producerService.UpdateProducerList(npwdUpdatedProducers);

            if (pEprApiResponse.IsSuccessStatusCode)
            {
                logger.LogInformation($"Producers list successfully updated in NPWD for time period {fromDate} to {toDate}.");
            }
            else
            {
                logger.LogError($"Failed to update producers list in NPWD. Status Code: {pEprApiResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while updating the producers list: {ex.Message}");
        }
    }
}