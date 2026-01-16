using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Extensions;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Services;

public class ProducerEmailService(
    ILogger<ProducerEmailService> logger,
    IOrganisationService organisationService,
    IEmailService emailService
) : IProducerEmailService
{
    public async Task SendEmailToProducersAsync(
        SavePrnDetailsRequest request,
        WoApiOrganisation? woOrganisation
    )
    {
        if (woOrganisation?.Id is null)
        {
            logger.LogError(
                "For prn {PrnNumber} Cannot send email to producer, IssueToOrganisation.Id is null",
                request.PrnNumber
            );
            return;
        }

        if (!int.TryParse(request.AccreditationYear, out var year))
        {
            logger.LogError(
                "For prn {PrnNumber} Cannot send email to producer, AccreditationYear is not valid: {year}",
                request.PrnNumber,
                request.AccreditationYear
            );
            return;
        }

        string organisationId = woOrganisation.Id.ToString();
        string? issuedToEntityTypeCode = woOrganisation.GetEntityTypeCode(year, logger);
        if (issuedToEntityTypeCode is null)
        {
            logger.LogError(
                "For prn {PrnNumber} Cannot send email to producer, failed to get issuedToEntityTypeCode",
                request.PrnNumber
            );
            return;
        }

        try
        {
            // Get list of producers
            var producerEmails =
                await organisationService.GetPersonEmailsAsync(
                    organisationId,
                    issuedToEntityTypeCode,
                    CancellationToken.None
                ) ?? [];

            logger.LogInformation(
                "Fetched {ProducerCount} producers for OrganisationId: {EPRId}",
                producerEmails.Count,
                organisationId
            );

            var producers = producerEmails.Select(p => CreateProducerEmail(p, request)).ToList();

            logger.LogInformation(
                "Sending email notifications to {ProducerCount} producers.",
                producers.Count
            );

            if (request.PrnStatusId == (int)EprnStatus.CANCELLED)
            {
                emailService.SendCancelledPrnsNotificationEmails(producers, organisationId);
            }
            else
            {
                emailService.SendEmailsToProducers(producers, organisationId);
            }

            logger.LogInformation(
                "Successfully processed and sent emails for message Id: {PrnNumber}",
                request.PrnNumber
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send email notification for issued prn: {PrnNo} and EprId: {EprId}",
                request.PrnNumber,
                organisationId
            );
        }
    }

    private static ProducerEmail CreateProducerEmail(
        PersonEmail producer,
        SavePrnDetailsRequest request
    )
    {
        return new ProducerEmail
        {
            EmailAddress = producer.Email,
            FirstName = producer.FirstName,
            LastName = producer.LastName,
            NameOfExporterReprocessor = request.IssuedByOrg ?? "",
            NameOfProducerComplianceScheme = request.OrganisationName ?? "",
            PrnNumber = request.PrnNumber ?? "",
            Material = request.MaterialName!,
            Tonnage = request.TonnageValue ?? 0,
            IsExporter = request.IsExport ?? false,
        };
    }
}
