using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    public void SendUpdatePrnsErrorEmailToNpwd(string errorMessage)
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
}
