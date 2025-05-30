﻿using EprPrnIntegration.Api.Models;

namespace EprPrnIntegration.Common.Service;

public interface IEmailService
{
    void SendErrorEmailToNpwd(string errorMessage);
    void SendEmailsToProducers(List<ProducerEmail> producerEmails, string organisationId);
    void SendValidationErrorPrnEmail(string csvData, DateTime reportDate);

    /// <summary>
    /// Notify cancelled PRNs
    /// </summary>
    /// <param name="producerEmails"></param>
    /// <param name="organisationId"></param>
    void SendCancelledPrnsNotificationEmails(List<ProducerEmail> producerEmails, string organisationId);
    
    /// <summary>
    /// Inform NPWD about PRNs received
    /// </summary>
    /// <param name="reportDate">The date up until PRNs were received</param>
    /// <param name="reportCount">Number of PRNs</param>
    /// <param name="reportCsv">Individual PRN details in comma separated list</param>
    void SendIssuedPrnsReconciliationEmailToNpwd(DateTime reportDate, int reportCount, string reportCsv);

    /// <summary>
    /// Send Updated PRNs status reconciliation email
    /// </summary>
    /// <param name="reportDate"></param>
    /// <param name="reportCsv"></param>
    /// <param name="rowCount"></param>
    void SendUpdatedPrnsReconciliationEmailToNpwd(DateTime reportDate, string reportCsv, int rowCount);

    /// <summary>
    /// Inform NPWD about organisations updated
    /// </summary>
    /// <param name="reportDate">The date up until organisations were updated</param>
    /// <param name="reportCsv">Updated organisations data in CSV formatted string</param>
    void SendUpdatedOrganisationsReconciliationEmailToNpwd(DateTime reportDate, int reportDataRowsCount, string reportCsv);
}
