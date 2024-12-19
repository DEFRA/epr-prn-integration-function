using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Interfaces;

namespace EprPrnIntegration.Common.Service;

public class EmailService(
    INotificationClient notificationClient,
    IOptions<MessagingConfig> messagingConfig,
    ILogger<EmailService> logger)
    : IEmailService
{
    private readonly MessagingConfig _messagingConfig = messagingConfig.Value;
    private const string ExceptionLogMessageGeneric = "GOV UK NOTIFY ERROR. Method: SendEmail: {emailAddress} Template: {templateId}";

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

    public void SendUpdatePrnsErrorEmailToNpwd(string errorMessage)
    {
        var npwdEmailAddress = _messagingConfig.NpwdEmail;
        var templateId = _messagingConfig.NpwdEmailTemplateId;

        var parameters = new Dictionary<string, object>
        {
            { "emailAddress", npwdEmailAddress! },
            { "applicationName", Constants.Constants.ApplicationName },
            { "logId", Guid.NewGuid() }, // To be set to a proper AppInsights Log Id in future
            { "errorMessage", errorMessage },
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
}
