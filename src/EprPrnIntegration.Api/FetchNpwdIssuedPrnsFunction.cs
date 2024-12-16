using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Options;
using EprPrnIntegration.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Validators;

namespace EprPrnIntegration.Api
{
    public class FetchNpwdIssuedPrnsFunction
    {
        private readonly ILogger<FetchNpwdIssuedPrnsFunction> _logger;
        private readonly INpwdClient _npwdClient;
        private readonly IServiceBusProvider _serviceBusProvider;
        private readonly IEmailService _emailService;
        private readonly IOptions<FeatureManagementConfiguration> _featureConfig;
        private readonly IOrganisationService _organisationService;
        private readonly IPrnService _prnService;
        private readonly IValidator<NpwdPrn> _validator;
        private readonly IUtilities _utilities;
        private readonly IConfiguration _configuration;

        public FetchNpwdIssuedPrnsFunction(ILogger<FetchNpwdIssuedPrnsFunction> logger, INpwdClient npwdClient, IServiceBusProvider serviceBusProvider, IEmailService emailService, IOrganisationService organisationService, IPrnService prnService, IValidator<NpwdPrn> validator, IOptions<FeatureManagementConfiguration> featureConfig, IUtilities utilities, IConfiguration configuration)
        {
            _logger = logger;
            _npwdClient = npwdClient;
            _serviceBusProvider = serviceBusProvider;
            _emailService = emailService;
            _organisationService = organisationService;
            _prnService = prnService;
            _validator = validator;
            _featureConfig = featureConfig;
            _utilities = utilities;
            _configuration = configuration;
        }

        [Function("FetchNpwdIssuedPrnsFunction")]
        public async Task Run([TimerTrigger("%FetchNpwdIssuedPrns:Schedule%")] TimerInfo timerInfo)
        {
            bool isOn = _featureConfig.Value.RunIntegration ?? false;
            if (!isOn)
            {
                _logger.LogInformation("FetchNpwdIssuedPrnsFunction function is disabled by feature flag");
                return;
            }

            _logger.LogInformation($"FetchNpwdIssuedPrnsFunction function started at: {DateTime.UtcNow}");

            var deltaRun = await _utilities.GetDeltaSyncExecution(NpwdDeltaSyncType.FetchNpwdIssuedPrns);
            var toDate = DateTime.UtcNow;
            var filter = "(EvidenceStatusCode eq 'EV-CANCEL' or EvidenceStatusCode eq 'EV-AWACCEP' or EvidenceStatusCode eq 'EV-AWACCEP-EPR')";
            if (deltaRun != null && DateTime.TryParse(_configuration["DefaultLastRunDate"], out DateTime defaultLastRunDate) && deltaRun.LastSyncDateTime > defaultLastRunDate)
            {
                filter = $@"{filter} and ((StatusDate ge {deltaRun.LastSyncDateTime.ToUniversalTime():O} and StatusDate lt {toDate.ToUniversalTime():O}) or (ModifiedOn ge {deltaRun.LastSyncDateTime.ToUniversalTime():O} and ModifiedOn lt {toDate.ToUniversalTime():O}))";
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

            // prns from NPWD must pass validation
            var validNpwdIssuedPrns = new List<NpwdPrn>();
            foreach (NpwdPrn npwdPrn in npwdIssuedPrns)
            {
                var validator = new NpwdPrnValidator();
                var validationResult = validator.Validate(npwdPrn);
                if (validationResult != null && validationResult.IsValid)
                {
                    validNpwdIssuedPrns.Add(npwdPrn);
                    continue;
                }

                var errorMessages = string.Join(" | ", validationResult?.Errors?.Select(x => x.ErrorMessage) ?? []);
                var eventData = CreateCustomEvent(npwdPrn, errorMessages);
                _utilities.AddCustomEvent(CustomEvents.NpwdPrnValidationError, eventData);
            }

            if (validNpwdIssuedPrns.Count == 0)
            {
                _logger.LogWarning("Zero Prns in Npwd passed validation for filter {Filter}", filter);
                return;
            }

            try
            {
                await _serviceBusProvider.SendFetchedNpwdPrnsToQueue(validNpwdIssuedPrns);

                _logger.LogInformation("Issued Prns Pushed into Message Queue");

                await _utilities.SetDeltaSyncExecution(deltaRun, toDate);
                LogCustomEvents(validNpwdIssuedPrns);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed pushing issued prns in message queue with exception: {ex}", ex);
                throw;
            }

            try
            {
                await ProcessIssuedPrnsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed fetching prns from the queue with exception: {ex}", ex);
                throw;
            }

            _logger.LogInformation($"FetchNpwdIssuedPrnsFunction function Completed at: {DateTime.UtcNow}");
        }

        private void LogCustomEvents(List<NpwdPrn> npwdIssuedPrns)
        {
            foreach (var npwdPrn in npwdIssuedPrns)
            {
                var eventData = CreateCustomEvent(npwdPrn);
                _utilities.AddCustomEvent(CustomEvents.IssuedPrn, eventData);
            }
        }

        private Dictionary<string, string> CreateCustomEvent(NpwdPrn npwdPrn, string errorMessage = "")
        {
            Dictionary<string, string> eventData = new()
            {
                { "PRN Number", npwdPrn.EvidenceNo ?? "No PRN Number" },
                { "Incoming Status", npwdPrn.EvidenceStatusCode ?? "Blank Incoming Status" },
                { "Date", DateTime.UtcNow.ToString() },
                { "Organisaton Name", npwdPrn.IssuedToOrgName ?? "Blank Organisation Name"},
            };

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                eventData.Add("Error Comments", errorMessage);
            }

            return eventData;
        }

        internal async Task ProcessIssuedPrnsAsync()
        {
            while (true)
            {
                var messages = await _serviceBusProvider.ReceiveFetchedNpwdPrnsFromQueue();

                if (!messages.Any())
                {
                    _logger.LogInformation("No messages found in the queue. Exiting the processing loop.");
                    break;
                }

                foreach (var message in messages)
                {
                    var messageContent = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());

                    try
                    {
                        _logger.LogInformation("Validating message with Id: {MessageId}", message.MessageId);

                        var validationResult = _validator.Validate(messageContent!);

                        if (validationResult.IsValid)
                        {
                            try
                            {
                                _logger.LogInformation("Validation passed for message Id: {MessageId}. Processing the PRN.", message.MessageId);

                                // Save to PRN DB
                                var request = NpwdPrnToSavePrnDetailsRequestMapper.Map(messageContent!);
                                await _prnService.SavePrn(request);
                                _logger.LogInformation("Successfully saved PRN details for EvidenceNo: {EvidenceNo}", request.EvidenceNo);
                                await SendEmailToProducers(message, messageContent, request);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing message Id: {MessageId}. Adding it back to the queue.", message.MessageId);
                                await _serviceBusProvider.SendMessageBackToFetchPrnQueue(message);
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Validation failed for message Id: {MessageId}. Sending to error queue.", message.MessageId);
                            await _serviceBusProvider.SendMessageToErrorQueue(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error while processing message Id: {MessageId}.", message.MessageId);
                        throw;
                    }
                }
            }
        }

        private async Task SendEmailToProducers(ServiceBusReceivedMessage message, NpwdPrn? messageContent, SavePrnDetailsRequest request)
        {
            // Get list of producers
            var producerEmails = await _organisationService.GetPersonEmailsAsync(messageContent!.IssuedToEPRId!, CancellationToken.None);
            _logger.LogInformation("Fetched {ProducerCount} producers for OrganisationId: {EPRId}", producerEmails.Count, messageContent.IssuedToEPRId);

            var producers = new List<ProducerEmail>();
            foreach (var producer in producerEmails)
            {
                var producerEmail = new ProducerEmail
                {
                    EmailAddress = producer.Email,
                    FirstName = producer.FirstName,
                    LastName = producer.LastName,
                    NameOfExporterReprocessor = request.ReprocessorAgency!,
                    NameOfProducerComplianceScheme = request.IssuedToOrgName,
                    PrnNumber = request.EvidenceNo!,
                    Material = request.EvidenceMaterial!,
                    Tonnage = Convert.ToDecimal(request.EvidenceTonnes),
                    IsPrn = NpwdPrnToSavePrnDetailsRequestMapper.IsExport(request.EvidenceNo!)
                };
                producers.Add(producerEmail);
            }

            _logger.LogInformation("Sending email notifications to {ProducerCount} producers.", producers.Count);
            _emailService.SendEmailsToProducers(producers, messageContent!.IssuedToEPRId!);

            _logger.LogInformation("Successfully processed and sent emails for message Id: {MessageId}", message.MessageId);
        }
    }
}
