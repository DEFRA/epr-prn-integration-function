using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api
{
    public class FetchNpwdIssuedPrnsFunction
    {
        private readonly ILogger<FetchNpwdIssuedPrnsFunction> _logger;
        private readonly INpwdClient _npwdClient;
        private readonly IServiceBusProvider _serviceBusProvider;

        public FetchNpwdIssuedPrnsFunction(ILogger<FetchNpwdIssuedPrnsFunction> logger, INpwdClient npwdClient, IServiceBusProvider serviceBusProvider)
        {
            _logger = logger;
            _npwdClient = npwdClient;
            _serviceBusProvider = serviceBusProvider;
        }

        [Function("FetchNpwdIssuedPrnsFunction")]
        public async Task Run([TimerTrigger("%FetchNpwdIssuedPrns:Schedule%")] TimerInfo timerInfo)
        {
            _logger.LogInformation($"FetchNpwdIssuedPrnsFunction function started at: {DateTime.UtcNow}");

            var lastFetched = GetLastFetchedTime();
            var currentRunDateTime = DateTime.UtcNow;

            var filter = "(EvidenceStatusCode eq 'EV-CANCEL' or EvidenceStatusCode eq 'EV-AWACCEP' or EvidenceStatusCode eq 'EV-AWACCEP-EPR')";
            if (lastFetched != null)
            {
                filter = $"""{filter} and StatusDate ge {lastFetched.Value.ToUniversalTime():O} and StatusDate lt {currentRunDateTime.ToUniversalTime():O}""";
            }

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
                SetLastFetchTime(currentRunDateTime);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed pushing issued prns in message queue with exception: {ex}", ex);
                throw;
            }

            _logger.LogInformation($"FetchNpwdIssuedPrnsFunction function Completed at: {DateTime.UtcNow}");
        }

        private void SetLastFetchTime(DateTime currentRunDateTime)
        {
            _logger.LogInformation($"Setting CurrentRunDateTime For Future lastRun {currentRunDateTime:O}");
            //Pending pushing last run logic is done by Ehsan 
        }

        private DateTime? GetLastFetchedTime()
        {
            _logger.LogInformation("Getting Future lastRun DateTime to fetch data");
            //pending setting null as the logic to pull lastrun is going to be done by Ehsan
            //Make sure you set in utc format and parse in utc format
            return null;
        }
    }
}
