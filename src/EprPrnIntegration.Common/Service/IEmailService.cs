using EprPrnIntegration.Api.Models;

namespace EprPrnIntegration.Common.Service;

public interface IEmailService
{
    void SendErrorEmailToNpwd(string errorMessage);
    void SendEmailsToProducers(List<ProducerEmail> producerEmails, string organisationId);
    void SendValidationErrorPrnEmail(string csvData, DateTime reportDate);


    /// <summary>
    /// Inform NPWD about PRNs received
    /// </summary>
    /// <param name="reportDate">The date up until PRNs were received</param>
    /// <param name="reportCount">Number of PRNs</param>
    /// <param name="reportCsv">Individual PRN details in comma separated list</param>
    void SendIssuedPrnsReconciliationEmailToNpwd(DateTime reportDate, int reportCount, string reportCsv);
    void SendUpdatedPrnsReconciliationEmailToNpwd(DateTime reportDate, string reportCsv);
}
