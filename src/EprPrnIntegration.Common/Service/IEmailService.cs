using EprPrnIntegration.Api.Models;

namespace EprPrnIntegration.Common.Service;

public interface IEmailService
{
    void SendErrorEmailToNpwd(string errorMessage);
    void SendEmailsToProducers(List<ProducerEmail> producerEmails, string organisationId);
    void SendErrorFetchedPrnEmail(Stream attachmentStream);
}
