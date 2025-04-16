using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Client;
using Notify.Interfaces;
using System.Diagnostics;

namespace EprPrnIntegration.Common.Service;

public class EmailService(
    INotificationClient notificationClient,
    IOptions<MessagingConfig> messagingConfig,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly MessagingConfig _messagingConfig = messagingConfig.Value;
    private const string ExceptionLogMessageGeneric = "GOV UK NOTIFY ERROR. Method: SendEmail: {emailAddress} Template: {templateId}";

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
                var response = notificationClient.SendEmail(producer.EmailAddress, templateId, parameters);
                string message = $"Email sent to {producer.FirstName} {producer.LastName} with email address {producer.EmailAddress} and the responseid is {response.id}.";
                logger.LogInformation(message);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, Constants.Values.ExceptionLogMessage, organisationId, templateId);
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
            var response = notificationClient.SendEmail(npwdEmailAddress, templateId, parameters);

            string message = $"Email sent to NPWD with email address {npwdEmailAddress} and the responseid is {response.id}.";
            logger.LogInformation(message);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, ExceptionLogMessageGeneric, npwdEmailAddress, templateId);
        }
    }

    public void SendValidationErrorPrnEmail(string csvData, DateTime reportDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvData, nameof(csvData));

        var templateId = _messagingConfig.NpwdValidationErrorsTemplateId;
        var emailAddress = _messagingConfig.NpwdEmail;

        var parameters = new Dictionary<string, object>
        {
            ["reportDate"] = reportDate.ToString("dd/MM/yyyy"), // Ensured UK date format
            ["link_to_file"] = csvData
        };

        var responseId = SendNpwdEmail(parameters, templateId, emailAddress);

        if (!string.IsNullOrWhiteSpace(responseId))
        {
            var message = $"Validation Error email sent to NPWD with email address {emailAddress} and the response ID is {responseId}.";
            logger.LogInformation(message);
        }
    }

    public void SendCancelledPrnsNotificationEmails(List<ProducerEmail> producerEmails, string organisationId)
    {
        var templateId = _messagingConfig.NpwdCancelledPrnsNotificationTemplateId;

        foreach (var producer in producerEmails)
        {
            var prnPern = producer.IsExporter ? "PERN" : "PRN";
            var parameters = new Dictionary<string, object>
            {
                ["emailAddress"] = producer.EmailAddress,
                ["prnNumber"] = producer.PrnNumber,
                ["PrnPern"] = prnPern,
                ["firstName"] = producer.FirstName,
                ["lastName"] = producer.LastName
            };

            try
            {
                var response = notificationClient.SendEmail(producer.EmailAddress, templateId, parameters);
                string message = $"Email sent to email address {producer.EmailAddress} and the responseid is {response.id}.";
                logger.LogInformation(message);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, Constants.Values.ExceptionLogMessage, organisationId, templateId);
            }
        }
    }

    public void SendIssuedPrnsReconciliationEmailToNpwd(DateTime reportDate, int reportCount, string reportCsv)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportCsv, nameof(reportCsv));

        var templateId = _messagingConfig.NpwdReconcileIssuedPrnsTemplateId;
        var emailAddress = _messagingConfig.NpwdEmail;

        var messagePersonalisation = new Dictionary<string, object>
        {
            ["report_date"] = reportDate.ToString("dd/MM/yyyy"),
            ["report_count"] = reportCount,
            ["csvData"] = reportCsv
        };

        var responseId = SendNpwdEmail(messagePersonalisation, templateId, emailAddress);

        if (!string.IsNullOrWhiteSpace(responseId))
        {
            var message = $"Reconciliation email sent to NPWD with email address {emailAddress} and the response id is {responseId}.";
            logger.LogInformation(message);
        }
    }

    public void SendUpdatedPrnsReconciliationEmailToNpwd(DateTime reportDate, string reportCsv, int rowCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportCsv, nameof(reportCsv));

        var templateId = _messagingConfig.NpwdReconcileUpdatedPrnsTemplateId;
        var emailAddress = _messagingConfig.NpwdEmail;

        var messagePersonalisation = new Dictionary<string, object>
        {
            ["date"] = reportDate.ToString("dd/MM/yyyy"),
            ["csvData"] = reportCsv,
            ["row_count"] = rowCount
        };

        var responseId = SendNpwdEmail(messagePersonalisation, templateId, emailAddress);

        if (!string.IsNullOrWhiteSpace(responseId))
        {
            logger.LogInformation($"Reconciliation email sent to NPWD with email address {emailAddress} and the response id is {responseId}.");
        }
    }

    public void SendUpdatedOrganisationsReconciliationEmailToNpwd(DateTime reportDate, int reportDataRowsCount, string reportCsv)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportCsv, nameof(reportCsv));

        var templateId = _messagingConfig.NpwdReconcileUpdatedOrganisationsTemplateId;
        var emailAddress = _messagingConfig.NpwdEmail;

        var messagePersonalisation = new Dictionary<string, object>
        {
            ["UpdatedDate"] = reportDate.ToString("dd/MM/yyyy"),
            ["RowCount"] = reportDataRowsCount,
            ["csvData"] = reportCsv
        };

        var responseId = SendNpwdEmail(messagePersonalisation, templateId, emailAddress);

        if (!string.IsNullOrWhiteSpace(responseId))
        {
            var message = $"Updated organisations reconciliation email sent to NPWD with email address {emailAddress} and the response id is {responseId}.";
            logger.LogInformation(message);
        }
    }

    #region Private Methods
    private string SendNpwdEmail(Dictionary<string, object> data, string templateId, string emailAddress)
    {
        try
        {
            var response = notificationClient.SendEmail(emailAddress, templateId, data);
            return response.id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {EmailAddress} using template ID {TemplateId}", emailAddress, templateId);
            throw;
        }
    }
    #endregion
}
