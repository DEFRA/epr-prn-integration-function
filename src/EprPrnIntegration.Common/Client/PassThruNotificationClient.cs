using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Notify.Interfaces;
using Notify.Models;
using Notify.Models.Responses;

namespace EprPrnIntegration.Common.Client;

[ExcludeFromCodeCoverage]
public class PassThruNotificationClient(ILogger<INotificationClient> logger) : INotificationClient
{
    public EmailNotificationResponse SendEmail(
        string emailAddress,
        string templateId,
        Dictionary<string, dynamic> personalisation = null!,
        string clientReference = null!,
        string emailReplyToId = null!,
        string oneClickUnsubscribeURL = null!
    )
    {
        logger.LogError(
            "PassThruNotificationClient::SendEmail is a dummy implementation intended for local testing. If you're reading this log from a cloud workload, the system is misconfigured."
        );

        return new EmailNotificationResponse();
    }

    public Task<string> GET(string url)
    {
        throw new NotImplementedException();
    }

    public Task<string> POST(string url, string json)
    {
        throw new NotImplementedException();
    }

    public Task<string> MakeRequest(string url, HttpMethod method, HttpContent content = null!)
    {
        throw new NotImplementedException();
    }

    public Tuple<string, string> ExtractServiceIdAndApiKey(string fromApiKey)
    {
        throw new NotImplementedException();
    }

    public Uri ValidateBaseUri(string baseUrl)
    {
        throw new NotImplementedException();
    }

    public string GetUserAgent()
    {
        throw new NotImplementedException();
    }

    public TemplatePreviewResponse GenerateTemplatePreview(
        string templateId,
        Dictionary<string, dynamic> personalisation = null!
    )
    {
        throw new NotImplementedException();
    }

    public TemplateList GetAllTemplates(string templateType = "")
    {
        throw new NotImplementedException();
    }

    public Notification GetNotificationById(string notificationId)
    {
        throw new NotImplementedException();
    }

    public NotificationList GetNotifications(
        string templateType = "",
        string status = "",
        string reference = "",
        string olderThanId = "",
        bool includeSpreadsheetUploads = false
    )
    {
        throw new NotImplementedException();
    }

    public ReceivedTextListResponse GetReceivedTexts(string olderThanId = "")
    {
        throw new NotImplementedException();
    }

    public TemplateResponse GetTemplateById(string templateId)
    {
        throw new NotImplementedException();
    }

    public TemplateResponse GetTemplateByIdAndVersion(string templateId, int version = 0)
    {
        throw new NotImplementedException();
    }

    public SmsNotificationResponse SendSms(
        string mobileNumber,
        string templateId,
        Dictionary<string, dynamic> personalisation = null!,
        string clientReference = null!,
        string smsSenderId = null!
    )
    {
        throw new NotImplementedException();
    }

    public LetterNotificationResponse SendLetter(
        string templateId,
        Dictionary<string, dynamic> personalisation,
        string clientReference = null!
    )
    {
        throw new NotImplementedException();
    }

    public LetterNotificationResponse SendPrecompiledLetter(
        string clientReference,
        byte[] pdfContents,
        string postage = null!
    )
    {
        throw new NotImplementedException();
    }
}
