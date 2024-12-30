using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Interfaces;
using Notify.Models;
using System.Text;

namespace EprPrnIntegration.Common.Service;

public class EmailService : IEmailService
{
    private readonly MessagingConfig _messagingConfig;
    private readonly INotificationClient _notificationClient;
    private readonly ILogger<EmailService> _logger;

    public EmailService(INotificationClient notificationClient, IOptions<MessagingConfig> messagingConfig, ILogger<EmailService> logger)
    {
        _notificationClient = notificationClient;
        _messagingConfig = messagingConfig.Value;
        _logger = logger;
    }
    public void SendErrorSummaryEmailNew(List<Dictionary<string, string>> errorList1)
    {
        try
        {
            // Sample error list, replace with actual input
            var errorList = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "PRN Number", "123456" },
                { "Incoming Status", "Pending" },
                { "Date", DateTime.UtcNow.ToString() },
                { "Organisation Name", "Org A" },
                { "Error Comments", "Error Comments1" }
            },
            new Dictionary<string, string>
            {
                { "PRN Number", "789012" },
                { "Incoming Status", "Completed" },
                { "Date", DateTime.UtcNow.ToString() },
                { "Organisation Name", "Org B" },
                { "Error Comments", "Error Comments2" }
            },
            new Dictionary<string, string>
            {
                { "PRN Number", "345678" },
                { "Incoming Status", "Failed" },
                { "Date", DateTime.UtcNow.ToString() },
                { "Organisation Name", "Org C" },
                { "Error Comments", "Error Comments3" }
            }
        };

            // Build an HTML table for the email    
            var errorTable = new StringBuilder();
            errorTable.Append("<table border='1' style='border-collapse:collapse;width:100%;'>");
            errorTable.Append("<thead><tr>");

            // Add table headers (assumes all dictionaries have the same keys)
            foreach (var header in errorList[0].Keys)
            {
                errorTable.Append($"<th style='padding:8px;text-align:left;background-color:#f2f2f2;'>{header}</th>");
            }

            errorTable.Append("</tr></thead>");
            errorTable.Append("<tbody>");

            // Add rows for each error
            foreach (var error in errorList)
            {
                errorTable.Append("<tr>");
                foreach (var value in error.Values)
                {
                    errorTable.Append($"<td style='padding:8px;'>{value}</td>");
                }
                errorTable.Append("</tr>");
            }

            errorTable.Append("</tbody></table>");

            // Email parameters
            var parameters = new Dictionary<string, object>
        {
            { "emailAddress", _messagingConfig.NpwdSupportEmail! },
            { "ApplicationName", "PRN" },
            { "OperationId", "ops1" },
            { "ErrorMessage", errorTable.ToString() }
        };

            // Send the email
            var response = _notificationClient.SendEmail(
                _messagingConfig.NpwdSupportEmail,
                _messagingConfig.ErrorMessagesTemplateId,
                parameters
                //,isHtml: true
            );

            string message = "Error Scenarios Email sent to NPWD support.";
            _logger.LogInformation(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, Constants.Values.ExceptionLogMessage, "", _messagingConfig.ErrorMessagesTemplateId);
        }
    }


    //var parameters = new Dictionary<string, object>
    //                    {
    //                        { "emailAddress", _messagingConfig.NpwdSupportEmail! },
    //                        { "ApplicationName", "PRN" },
    //                        { "OperationId", "ops1" },
    //                        { "ErrorMessage", errorList.ToString() },
    //                    };
    //var response = _notificationClient.SendEmail(_messagingConfig.ErrorMessagesTemplateId!, _messagingConfig.ErrorMessagesTemplateId, parameters);
    //string message = $"Error Scenarios Email sent to NPWD support.";
    //_logger.LogInformation(message);
    // Construct the formatted error messages as a string

    public void SendErrorSummaryEmail(List<Dictionary<string, string>> errorList1)
    {
        try
        {
            var errorList = new List<Dictionary<string, string>>
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

            foreach (var error in errorList)
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
}
