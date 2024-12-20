using EprPrnIntegration.Api.Models;

namespace EprPrnIntegration.Common.Service;

public interface IEmailService
{
    void SendUpdatePrnsErrorEmailToNpwd(string errorMessage);
    void SendEmailsToProducers(List<ProducerEmail> producerEmails, string organisationId);
}
