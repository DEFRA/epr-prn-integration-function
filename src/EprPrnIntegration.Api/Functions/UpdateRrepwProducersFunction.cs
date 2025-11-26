using System.Net;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

public class UpdateRrepwProducersFunction(
    ICommonDataService commonDataService,
    IRrepwClient rrepwClient,
    ILogger<UpdateRrepwProducersFunction> logger,
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

        var newestProducerStatusDate = updatedEprProducers.Select(x => x.UpdatedDateTime).LastOrDefault();
        if (newestProducerStatusDate.GetValueOrDefault() > DateTime.MinValue)
            toDate = newestProducerStatusDate.GetValueOrDefault().ToUniversalTime();

        logger.LogInformation("Received a total of {ProducerCount} producer to RREPW for updating",
            updatedEprProducers.Count);
        
        foreach (var updatedProducer in updatedEprProducers)
        {
            var request = ProducerUpdateRequestMapper.Map(updatedProducer);

            await rrepwClient.Patch(request);
        }
        
        await utilities.SetDeltaSyncExecution(deltaRun, toDate);
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