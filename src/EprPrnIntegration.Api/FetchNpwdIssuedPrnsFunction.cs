using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api
{
    public class FetchNpwdIssuedPrnsFunction
    {
        private readonly ILogger<FetchNpwdIssuedPrnsFunction> _logger;
        private readonly INpwdClient _npwdClient;
        private readonly IServiceBusProvider _serviceBusProvider;
        private readonly IConfiguration _configuration;

        public FetchNpwdIssuedPrnsFunction(ILogger<FetchNpwdIssuedPrnsFunction> logger, INpwdClient npwdClient, IServiceBusProvider serviceBusProvider, IConfiguration configuration)
        {
            _logger = logger;
            _npwdClient = npwdClient;
            _serviceBusProvider = serviceBusProvider;
            _configuration = configuration;
        }

        [Function("FetchNpwdIssuedPrnsFunction")]
        public async Task Run([TimerTrigger("%FetchNpwdIssuedPrns:Schedule%")] TimerInfo timerInfo)
        {

            if (!bool.TryParse(_configuration[ConfigSettingKeys.RunIntegrationFeatureFlag], out bool isOn))
            {
                isOn = false;
            }

            if (!isOn)
            {
                _logger.LogInformation("FetchNpwdIssuedPrnsFunction function is turned off");
                return;
            }

            _logger.LogInformation($"FetchNpwdIssuedPrnsFunction function started at: {DateTime.UtcNow}");

            //pending find out the filter criteria and add it
            var filter = "1 eq 1";

            var npwdIssuedPrns = new List<NpwdPrn>();
            try
            {
                npwdIssuedPrns = await _npwdClient.GetIssuedPrns(filter);
                if (npwdIssuedPrns == null || npwdIssuedPrns.Count == 0)
                {
                    _logger.LogWarning($"No Prns Exists in npwd for filter {filter}");
                    return;
                }
                _logger.LogInformation("Total: {Count} fetched from Npwd with filter {filter}", npwdIssuedPrns.Count,filter);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Failed Get Prns from npwd for filter {filter} with exception {ex}", filter, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"Failed Get Prns method for filter {filter} with exception {ex}", filter, ex.Message);
                throw;
            }


            try
            {
                await _serviceBusProvider.SendFetchedNpwdPrnsToQueue(npwdIssuedPrns);
                _logger.LogInformation("Issued Prns Pushed into Message Queue");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed pushing issued prns in message queue with exception: {ex}", ex);
                throw;
            }

            _logger.LogInformation($"FetchNpwdIssuedPrnsFunction function Completed at: {DateTime.UtcNow}");
        }
    }
}
