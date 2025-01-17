using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Client;
using Notify.Interfaces;
using System.Diagnostics;

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
            var templateId = producer.IsExporter ? _messagingConfig.PernTemplateId : _messagingConfig.PrnTemplateId;
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
                _logger.LogInformation("Email sent to {FirstName} {LastName} with email address {EmailAddress} and the responseid is {Id}.", producer.FirstName, producer.LastName, producer.EmailAddress, response.id);
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
            _logger.LogInformation("Email sent to NPWD with email address {npwdEmailAddress} and the response ID is {response.id}.", npwdEmailAddress, response.id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ExceptionLogMessageGeneric, npwdEmailAddress, templateId);
        }
    }

    public void SendValidationErrorPrnEmail(Stream attachmentStream, DateTime reportDate)
    {
        ArgumentNullException.ThrowIfNull(attachmentStream);

        var npwdEmailAddress = _messagingConfig.NpwdEmail;
        var templateId = _messagingConfig.NpwdValidationErrorsTemplateId;

        attachmentStream.Position = 0;

        using var memoryStream = new MemoryStream();
        attachmentStream.CopyTo(memoryStream);
        var fileBytes = memoryStream.ToArray();
        var fileUpload = NotificationClient.PrepareUpload(fileBytes, $"error_events{DateTime.UtcNow.ToShortDateString()}.csv");
        
        var parameters = new Dictionary<string, object>
        {
            { "emailAddress", npwdEmailAddress! },
            { "reportDate", reportDate! },
            { "link_to_file", fileUpload }
        };

        try
        {
            var response = _notificationClient.SendEmail(npwdEmailAddress, templateId, parameters);
            _logger.LogInformation("Email sent to NPWD with email address {npwdEmailAddress} and the response ID is {response.id}.", npwdEmailAddress, response.id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {EmailAddress} using template ID {TemplateId}", npwdEmailAddress, templateId);
        }
    }

    public void SendIssuedPrnsReconciliationEmailToNpwd(DateTime reportDate, int reportCount, string reportCsv)
    {
        var npwdEmailAddress = _messagingConfig.NpwdEmail;
        var templateId = _messagingConfig.NpwdReconcileIssuedPrnsTemplateId;
        string filename = string.Format("issuedprns_{0:yyyyMMdd}.csv", reportDate);

        Dictionary<string, object> messagePersonalisation = new Dictionary<string, object>
        {
            {
                "report_date", reportDate.ToString("dd/MM/yyyy")
            },
            {
                "report_count", reportCount
            },
            {
                "link_to_file", NotificationClient.PrepareUpload(System.Text.Encoding.UTF8.GetBytes(reportCsv), filename)
            }
        };

        try
        {
            var response = _notificationClient.SendEmail(npwdEmailAddress, templateId, messagePersonalisation);
            _logger.LogInformation("Reconciliation email sent to NPWD with email address {EmailAddress} and the responseid is {Id}.", npwdEmailAddress, response.id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {EmailAddress} using template ID {TemplateId}", npwdEmailAddress, templateId);
        }
    }
}
