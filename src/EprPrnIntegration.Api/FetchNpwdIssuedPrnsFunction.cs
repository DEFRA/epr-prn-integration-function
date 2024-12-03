using EprPrnIntegration.Api.Mappers;
using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api
{
    public class FetchNpwdIssuedPrnsFunction
    {
        private readonly ILogger<FetchNpwdIssuedPrnsFunction> _logger;
        private readonly INpwdClient _npwdClient;
        private readonly IServiceBusProvider _serviceBusProvider;
        private readonly IEmailService _emailService;
        private readonly IOrganisationService _organisationService;
        private readonly IPrnService _prnService;
        public FetchNpwdIssuedPrnsFunction(ILogger<FetchNpwdIssuedPrnsFunction> logger, INpwdClient npwdClient, IServiceBusProvider serviceBusProvider, IEmailService emailService, IOrganisationService organisationService, IPrnService prnService)
        {
            _logger = logger;
            _npwdClient = npwdClient;
            _serviceBusProvider = serviceBusProvider;
            _emailService = emailService;
            _organisationService = organisationService;
            _prnService = prnService;
        }

        //[Function("FetchNpwdIssuedPrnsFunction")]
        //public async Task Run([TimerTrigger("%FetchNpwdIssuedPrns:Schedule%")] TimerInfo timerInfo)
        [Function("TriggerFetchNpwdIssuedPrnsFunction")]
        public async Task TriggerFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation($"FetchNpwdIssuedPrnsFunction function started at: {DateTime.UtcNow}");

            var lastFetched = await GetLastFetchedTimeAsync();
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
                _logger.LogInformation("Total: {Count} fetched from Npwd with filter {filter}", npwdIssuedPrns.Count, filter);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Failed Get Prns from npwd for filter {filter} with exception {ex}", filter, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed Get Prns method for filter {filter} with exception {ex}", filter, ex.Message);
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

            //Read from SB
            var prns = new List<NpwdPrn>();
            try
            {
                //Todo: 
                //1. Fetch last run from service bus
                //2. Implement First run and subsequest run
                //3. Fetch PRNS
                prns = await _serviceBusProvider.ReceiveFetchedNpwdPrnsFromQueue(lastFetched, DateTime.Now);



                _logger.LogInformation("Issued Prns Pushed into Message Queue");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed fetchinh prns from the queue with exception: {ex}", ex);
                throw;
            }

            if (prns.Any())
            {
                var producerEmails = new List<PersonEmail>();
                foreach (var prn in prns)
                {
                    //4. and call Hims Api to save in Prn DB
                    var request = NpwdPrnToSavePrnDetailsRequestMapper.Map(prn);
                    var result = await _prnService.SavePrn(request);
                    //5. If PRN's exist Then get list of producers based on the organisationId (prn.IssuedToEPRId) for the PRN
                    producerEmails = await _organisationService.GetPersonEmailsAsync(prn.IssuedToEPRId, CancellationToken.None);
                }
                //6. Send Email as below
                var producers = new List<ProducerEmail>
            {
                new ProducerEmail
                {
                    EmailAddress = "venkata.rangala.external@eviden.com",
                    FirstName = "Venkat",
                    LastName = "Rangala",
                    NameOfExporterReprocessor = "Exporter1",
                    NameOfProducerComplianceScheme = "Scheme1",
                    PrnNumber = "12345",
                    Material = "Plastic",
                    Tonnage = 1000,
                    IsPrn = true
                }
            };
                _emailService.SendEmailsToProducers(producers, "org123");
            }

            _logger.LogInformation($"FetchNpwdIssuedPrnsFunction function Completed at: {DateTime.UtcNow}");
        }

        private void SetLastFetchTime(DateTime currentRunDateTime)
        {
            _logger.LogInformation($"Setting CurrentRunDateTime For Future lastRun {currentRunDateTime:O}");
            //Pending pushing last run logic is done by Ehsan 
        }

        private async Task<DateTime?> GetLastFetchedTimeAsync()
        {
            _logger.LogInformation("Getting Future lastRun DateTime to fetch data");
            //pending setting null as the logic to pull lastrun is going to be done by Ehsan
            //Make sure you set in utc format and parse in utc format
            var lastRunDateTime = await _serviceBusProvider.GetLastRunDateTimeFromQueue();

            if (lastRunDateTime.HasValue)
            {
                _logger.LogInformation("Last run datetime: {LastRunDateTime}", lastRunDateTime.Value);
                return lastRunDateTime;
            }
            else
            {
                _logger.LogWarning("No last run datetime found in the queue.");
            }

            return null;
        }
    }
}
