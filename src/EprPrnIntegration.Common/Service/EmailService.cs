using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Interfaces;
using Notify.Models;
using System.Diagnostics;
using System.Text;

namespace EprPrnIntegration.Common.Service;

public class EmailService : IEmailService
{
    private readonly MessagingConfig _messagingConfig;
    private readonly INotificationClient _notificationClient;
    private readonly ILogger<EmailService> _logger;
    private const string ExceptionLogMessageGeneric = "GOV UK NOTIFY ERROR. Method: SendEmail: {emailAddress} Template: {templateId}";

    public EmailService(INotificationClient notificationClient, IOptions<MessagingConfig> messagingConfig, ILogger<EmailService> logger)
    {
        _notificationClient = notificationClient;
        _messagingConfig = messagingConfig.Value;
        _logger = logger;
    }

    public void SendEmailsToProducers(List<ProducerEmail> producerEmails, string organisationId)
    {
        foreach (var producer in producerEmails)
        {
            var templateId = producer.IsPrn ? _messagingConfig.PrnTemplateId : _messagingConfig.PernTemplateId;
            var parameters = new Dictionary<string, object>
                                {
                                    { "emailAddress", producer.EmailAddress },
                                    { "firstName", producer.FirstName },
                                    { "lastName", producer.LastName },
                                    { "nameOfExporterReprocessor", producer.NameOfExporterReprocessor },
                                    { "nameOfProducerComplianceScheme", producer.NameOfProducerComplianceScheme },
                                    { "prnNumber", producer.PrnNumber },
                                    { "material", producer.Material },
                                    { "tonnage", producer.Tonnage }
                                };

            try
            {
                var response = _notificationClient.SendEmail(producer.EmailAddress, templateId, parameters);
                string message = $"Email sent to {producer.FirstName} {producer.LastName} with email address {producer.EmailAddress} and the responseid is {response.id}.";
                _logger.LogInformation(message);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Constants.Values.ExceptionLogMessage, organisationId, templateId);
            }
        }
    }

    public void SendErrorEmailToNpwd(string errorMessage)
    {
        var npwdEmailAddress = _messagingConfig.NpwdEmail;
        var templateId = _messagingConfig.NpwdEmailTemplateId;
        var operationId = Activity.Current?.RootId ?? string.Empty;

        var parameters = new Dictionary<string, object>
        {
                                    { "emailAddress", npwdEmailAddress! },
                                    { "ApplicationName", Constants.Constants.ApplicationName },
                                    { "OperationId", operationId },
                                    { "ErrorMessage", errorMessage },
                                };

        try
        {
            var response = _notificationClient.SendEmail(npwdEmailAddress, templateId, parameters);

            string message = $"Email sent to NPWD with email address {npwdEmailAddress} and the responseid is {response.id}.";
            _logger.LogInformation(message);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ExceptionLogMessageGeneric, npwdEmailAddress, templateId);
        }
    }

    public void SendErrorSummaryEmail(List<Dictionary<string, string>> errorList)
    {
        try
        {
            var errorListTemp = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>
                    {
                        { "PRN Number", "123456" },
                        { "Incoming Status", "Pending" },
                        { "Date", DateTime.UtcNow.ToString() },
                        { "Organisaton Name", "Org A" },
                        {"Error Comments", "Error Comments1" }
                    },
                    new Dictionary<string, string>
                    {
                        { "PRN Number", "789012" },
                        { "Incoming Status", "Completed" },
                        { "Date", DateTime.UtcNow.ToString() },
                        { "Organisaton Name", "Org B" },
                        {"Error Comments", "Error Comments2" }
                    },
                    new Dictionary<string, string>
                    {
                        { "PRN Number", "345678" },
                        { "Incoming Status", "Failed" },
                        { "Date", DateTime.UtcNow.ToString() },
                        { "Organisaton Name", "Org C" },
                        {"Error Comments", "Error Comments3" }
                    },
                    new Dictionary<string, string>
                    {
                        { "PRN Number", "901234" },
                        { "Incoming Status", "In Progress" },
                        { "Date", DateTime.UtcNow.ToString() },
                        { "Organisaton Name", "Org D" },
                        {"Error Comments", "Error Comments4" }
                    },
                    new Dictionary<string, string>
                    {
                        { "PRN Number", "567890" },
                        { "Incoming Status", "Not Started" },
                        { "Date", DateTime.UtcNow.ToString() },
                        { "Organisaton Name", "Org E" },
                        {"Error Comments", "Error Comments5" }
                    }
                };

            var errorMessages = new StringBuilder();

            foreach (var error in errorListTemp)
            {
                errorMessages.AppendLine(); // Add a blank line between each error block
                foreach (var kvp in error)
                {
                    errorMessages.AppendLine($"{kvp.Key}: {kvp.Value}");
                }

            }

            var parameters = new Dictionary<string, object>
                            {
                                { "emailAddress", _messagingConfig.NpwdSupportEmail! },
                                { "ApplicationName", "PRN" },
                                { "OperationId", "ops1" },
                                { "ErrorMessage", errorMessages.ToString() }
                            };

            var response = _notificationClient.SendEmail(_messagingConfig.NpwdSupportEmail, _messagingConfig.ErrorMessagesTemplateId, parameters);
            string message = $"Error Scenarios Email sent to NPWD support.";
            _logger.LogInformation(message);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, Constants.Values.ExceptionLogMessage, "", _messagingConfig.ErrorMessagesTemplateId);
        }
    }
}
