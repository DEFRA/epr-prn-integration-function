using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
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
using System.Net;
using EprPrnIntegration.Common.Models.Queues;
using System.Globalization;

namespace EprPrnIntegration.Api.Functions
{
    public class FetchNpwdIssuedPrnsFunction
    {
        private readonly ICoreServices _core;
        private readonly IMessagingServices _messaging;

        private readonly ILogger<FetchNpwdIssuedPrnsFunction> _logger;
        private readonly IOptions<FeatureManagementConfiguration> _featureConfig;
        private readonly IValidator<NpwdPrn> _validator;
        private readonly IUtilities _utilities;
        private readonly IConfiguration _configuration;

        public FetchNpwdIssuedPrnsFunction(ILogger<FetchNpwdIssuedPrnsFunction> logger, ICoreServices core, IMessagingServices messaging, IValidator<NpwdPrn> validator, IOptions<FeatureManagementConfiguration> featureConfig, IUtilities utilities, IConfiguration configuration)
        {
            _logger = logger;
            _core = core;
            _messaging = messaging;
            _validator = validator;
            _featureConfig = featureConfig;
            _utilities = utilities;
            _configuration = configuration;
        }

        [Function("FetchNpwdIssuedPrnsFunction")]
        public async Task Run([TimerTrigger("%FetchNpwdIssuedPrns:Schedule%")] TimerInfo timerInfo)
        {
            if (!IsFeatureEnabled())
                return;

            _logger.LogInformation("FetchNpwdIssuedPrnsFunction function started at: {DateTime}", DateTime.UtcNow);

            var deltaRun = await _utilities.GetDeltaSyncExecution(NpwdDeltaSyncType.FetchNpwdIssuedPrns);

            var now = DateTime.UtcNow;
            var toDate = _utilities.OffsetDateTimeWithLag(now, _configuration["FetchNpwdPrnsPollingLagSeconds"]);
            if (!toDate.Equals(now))
            {
                _logger.LogInformation("Upper date range {Now} rolled back to {ToDate}", now, toDate);
            }

            _logger.LogInformation("Fetching From: {FromDate} and To {ToDate} dates for this execution", deltaRun.LastSyncDateTime, toDate);

            var filter = GetFilterToFetchPrns(deltaRun, toDate);

            var npwdIssuedPrns = await FetchPrns(filter);

            await PushPrnsToInputQueue(npwdIssuedPrns);

            LogCustomEvents(npwdIssuedPrns);

            await _utilities.SetDeltaSyncExecution(deltaRun!, toDate);

            var validationFailedPrns = await _messaging.ServiceBusProvider.ProcessFetchedPrns(ProcessFetchedPrn);

            if (validationFailedPrns != null && validationFailedPrns.Count != 0)
            {
                SendErrorFetchedPrnEmail(validationFailedPrns);
            }

            _logger.LogInformation("FetchNpwdIssuedPrnsFunction function Completed at: {DateTime}", DateTime.UtcNow);

        }

        internal async Task<Dictionary<string, string>?> ProcessFetchedPrn(ServiceBusReceivedMessage message)
        {
            var evidenceNo = string.Empty;
            try
            {
                var messageContent = JsonSerializer.Deserialize<NpwdPrn>(message.Body.ToString());
                _logger.LogInformation("Validating message with Id: {MessageId}", message.MessageId);
                evidenceNo = messageContent?.EvidenceNo ?? "Missing";

                // prns sourced from NPWD must pass validation
                var validationResult = await _validator.ValidateAsync(messageContent!);
                if (validationResult.IsValid)
                {
                    try
                    {
                        _logger.LogInformation("Validation passed for message Id: {MessageId}. Processing the PRN.", message.MessageId);

                        // Save to PRN DB
                        var request = NpwdPrnToSavePrnDetailsRequestMapper.Map(messageContent!);
                        await _core.PrnService.SavePrn(request);
                        _logger.LogInformation("Successfully saved PRN details for EvidenceNo: {EvidenceNo}", request.EvidenceNo);
                        await SendEmailToProducers(message, messageContent, request);
                        var eventData = CreateCustomEvent(messageContent);
                        _utilities.AddCustomEvent(CustomEvents.InsertPrnOnEpr, eventData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message Id: {MessageId}. Adding it back to the queue.", message.MessageId);
                        await _messaging.ServiceBusProvider.SendMessageToErrorQueue(message, evidenceNo);
                    }
                }
                else
                {
                    _logger.LogWarning("Validation failed for message Id: {MessageId}. Sending to error queue.", message.MessageId);
                    var errorMessages = string.Join(" | ", validationResult.Errors.Select(x => x.ErrorMessage));
                    var eventData = CreateCustomEvent(messageContent, errorMessages);
                    _utilities.AddCustomEvent(CustomEvents.NpwdPrnValidationError, eventData);

                    await _messaging.ServiceBusProvider.SendMessageToErrorQueue(message, evidenceNo);
                    return eventData;
                }
                return null;
            }
            catch (Exception ex)
            {
                await _messaging.ServiceBusProvider.SendMessageToErrorQueue(message, evidenceNo);
                _logger.LogError(ex, "Unexpected error while processing message Id: {MessageId}.", message.MessageId);
                throw;
            }
        }

        private void LogCustomEvents(List<NpwdPrn> npwdIssuedPrns)
        {
            foreach (var npwdPrn in npwdIssuedPrns)
            {
                var eventData = CreateCustomEvent(npwdPrn);
                _utilities.AddCustomEvent(CustomEvents.IssuedPrn, eventData);
            }
        }

        private static Dictionary<string, string> CreateCustomEvent(NpwdPrn? npwdPrn, string errorMessage = "")
        {
            Dictionary<string, string> eventData = new()
            {
                { CustomEventFields.PrnNumber, npwdPrn?.EvidenceNo ?? "No PRN Number" },
                { CustomEventFields.IncomingStatus, npwdPrn?.EvidenceStatusCode ?? "Blank Incoming Status" },
                { CustomEventFields.Date, DateTime.UtcNow.ToString() },
                { CustomEventFields.OrganisationName, npwdPrn?.IssuedToOrgName ?? "Blank Organisation Name"},
            };

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                eventData.Add(CustomEventFields.ErrorComments, errorMessage);
            }

            return eventData;
        }

        private async Task SendEmailToProducers(ServiceBusReceivedMessage message, NpwdPrn? messageContent, SavePrnDetailsRequest request)
        {
            try
            {
                // Get list of producers
                var producerEmails = await _core.OrganisationService.GetPersonEmailsAsync(messageContent!.IssuedToEPRId!, messageContent.IssuedToEntityTypeCode!, CancellationToken.None) ?? [];
                _logger.LogInformation("Fetched {ProducerCount} producers for OrganisationId: {EPRId}", producerEmails.Count, messageContent.IssuedToEPRId);

                var producers = new List<ProducerEmail>();
                foreach (var producer in producerEmails)
                {
                    var producerEmail = new ProducerEmail
                    {
                        EmailAddress = producer.Email,
                        FirstName = producer.FirstName,
                        LastName = producer.LastName,
                        NameOfExporterReprocessor = request.IssuedByOrgName!,
                        NameOfProducerComplianceScheme = request.IssuedToOrgName!,
                        PrnNumber = request.EvidenceNo!,
                        Material = request.EvidenceMaterial!,
                        Tonnage = Convert.ToDecimal(request.EvidenceTonnes),
                        IsExporter = NpwdPrnToSavePrnDetailsRequestMapper.IsExport(request.EvidenceNo!)
                    };
                    producers.Add(producerEmail);
                }

                _logger.LogInformation("Sending email notifications to {ProducerCount} producers.", producers.Count);

                if (messageContent.EvidenceStatusCode == "EV-CANCEL")
                {
                    _messaging.EmailService.SendCancelledPrnsNotificationEmails(producers, messageContent!.IssuedToEPRId!);
                }
                else
                {
                    _messaging.EmailService.SendEmailsToProducers(producers, messageContent!.IssuedToEPRId!);
                }
             
                _logger.LogInformation("Successfully processed and sent emails for message Id: {MessageId}", message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification for issued prn: {PrnNo} and EprId: {EprId}", messageContent?.EvidenceNo,messageContent?.IssuedToEPRId);
            }
            
        }

        private void SendErrorFetchedPrnEmail(List<Dictionary<string, string>> validatedErrorMessages)
        {
            try
            {
                if (validatedErrorMessages.Count > 0)
                {
                    var dateTimeNow = DateTime.UtcNow;
                    var csvData = new Dictionary<string, List<string>>
                {
                    { CustomEventFields.PrnNumber, validatedErrorMessages.Select(kv => kv.GetValueOrDefault(CustomEventFields.PrnNumber, "No PRN Number")).ToList() },
                    { CustomEventFields.IncomingStatus, validatedErrorMessages.Select(kv => kv.GetValueOrDefault(CustomEventFields.IncomingStatus, "Blank Incoming Status")).ToList() },
                    { CustomEventFields.Date, validatedErrorMessages.Select(kv => kv.GetValueOrDefault(CustomEventFields.Date, dateTimeNow.ToString())).ToList() },
                    { CustomEventFields.OrganisationName, validatedErrorMessages.Select(kv => kv.GetValueOrDefault(CustomEventFields.OrganisationName, "Blank Organisation Name")).ToList() },
                    { CustomEventFields.ErrorComments, validatedErrorMessages.Select(kv => kv.GetValueOrDefault(CustomEventFields.ErrorComments, string.Empty)).ToList() }
                };

                    var csvContent = _utilities.CreateCsvContent(csvData);

                    _messaging.EmailService.SendValidationErrorPrnEmail(csvContent, dateTimeNow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending email for validation failed Prns");
            }
        }

        // check feature flag is enabled
        private bool IsFeatureEnabled()
        {
            bool isOn = _featureConfig.Value.RunIntegration ?? false;
            if (!isOn)
            {
                _logger.LogInformation("FetchNpwdIssuedPrnsFunction function is disabled by feature flag");
            }
            return isOn;
        }

        // get query string filter for NPWD endpoint
        private string GetFilterToFetchPrns(DeltaSyncExecution deltaRun, DateTime toDate)
        {
            var filter = "(EvidenceStatusCode eq 'EV-CANCEL' or EvidenceStatusCode eq 'EV-AWACCEP' or EvidenceStatusCode eq 'EV-AWACCEP-EPR')";
            if (deltaRun != null && DateTime.TryParseExact(_configuration["DefaultLastRunDate"], "yyyy-MM-dd",
                       CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime defaultLastRunDate) && deltaRun.LastSyncDateTime > defaultLastRunDate)
            {
                filter = $@"{filter} and ((StatusDate ge {deltaRun.LastSyncDateTime.ToUniversalTime():O} and StatusDate lt {toDate.ToUniversalTime():O}) or (ModifiedOn ge {deltaRun.LastSyncDateTime.ToUniversalTime():O} and ModifiedOn lt {toDate.ToUniversalTime():O}))";
            }
            _logger.LogInformation("Filter for fetching prns from npwd: {Filter}", filter);
            return filter;
        }

        // get Prns from NPWD
        private async Task<List<NpwdPrn>> FetchPrns(string filter)
        {
            List<NpwdPrn> npwdIssuedPrns;
            try
            {
                npwdIssuedPrns = await _core.NpwdClient.GetIssuedPrns(filter);
                if (npwdIssuedPrns == null || npwdIssuedPrns.Count == 0)
                {
                    _logger.LogWarning("No Prns Exists in npwd for filter {Filter}", filter);
                }
                else
                {
                    _logger.LogInformation("Total: {Count} fetched from Npwd with filter {Filter}", npwdIssuedPrns!.Count, filter);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed Get Prns from npwd for filter {Filter} with exception {Ex}", filter, ex);

                if (ex.StatusCode >= HttpStatusCode.InternalServerError || ex.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    _messaging.EmailService.SendErrorEmailToNpwd($"Failed to fetch issued PRNs. error code {ex.StatusCode} and raw response body: {ex.Message}");
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed Get Prns method for filter {Filter} with exception {Ex}", filter, ex.Message);
                throw;
            }

            return npwdIssuedPrns ?? [];
        }

        // place Prn message into a queue to await processing
        private async Task PushPrnsToInputQueue(List<NpwdPrn> npwdIssuedPrns)
        {
            try
            {
                await _messaging.ServiceBusProvider.SendFetchedNpwdPrnsToQueue(npwdIssuedPrns);
                _logger.LogInformation("Issued Prns Pushed into Message Queue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed pushing issued prns in message queue with exception: {Ex}", ex);
                throw;
            }
        }

    }
}
