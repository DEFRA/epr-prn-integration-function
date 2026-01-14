using System.Configuration;
using AutoMapper;
using Azure.Messaging.ServiceBus;
using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using EprPrnIntegration.Common.RESTServices;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EprPrnIntegration.Api.Functions;

public class FetchRrepwIssuedPrnsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<FetchRrepwIssuedPrnsFunction> logger,
    IRrepwService rrepwService,
    IPrnService prnService,
    IOptions<FetchRrepwIssuedPrnsConfiguration> config,
    ICoreServices core,
    IMessagingServices messaging,
    IWasteOrganisationsService woService
)
{
    private readonly IMapper _mapper = RrepwMappers.CreateMapper();

    [Function(FunctionName.FetchRrepwIssuedPrns)]
    public async Task Run(
        [TimerTrigger($"%{FunctionName.FetchRrepwIssuedPrns}:Trigger%")] TimerInfo _
    )
    {
        var lastUpdate = await GetLastUpdate();
        logger.LogInformation(
            "{FunctionId} resuming with last update time: {ExecutionDateTime}",
            FunctionName.FetchRrepwIssuedPrns,
            lastUpdate
        );

        var utcNow = DateTime.UtcNow;

        var prns = await rrepwService.ListPackagingRecyclingNotes(lastUpdate, utcNow);

        if (!prns.Any())
        {
            logger.LogInformation("No PRNs found from RREPW service; terminating.");
            return;
        }

        logger.LogInformation("Found {Count} PRN(s) to process", prns.Count);

        await ProcessPrns(prns, CancellationToken.None);

        await lastUpdateService.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, utcNow);
        logger.LogInformation(
            "{FunctionId} function completed at: {ExecutionDateTime}",
            FunctionName.FetchRrepwIssuedPrns,
            DateTime.UtcNow
        );
    }

    private async Task<DateTime> GetLastUpdate()
    {
        var lastUpdate = await lastUpdateService.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns);
        if (!lastUpdate.HasValue)
        {
            return DateTime.SpecifyKind(
                DateTime.ParseExact(
                    config.Value.DefaultStartDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                DateTimeKind.Utc
            );
        }
        return lastUpdate.Value;
    }

    private async Task SendEmailToProducers(
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

        string organisationId = woOrganisation.Id.ToString();
        string? issuedToEntityTypeCode = GetIssuedToEntityTypeCode(woOrganisation);
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
                await core.OrganisationService.GetPersonEmailsAsync(
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
                messaging.EmailService.SendCancelledPrnsNotificationEmails(
                    producers,
                    organisationId
                );
            }
            else
            {
                messaging.EmailService.SendEmailsToProducers(producers, organisationId);
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

    private string? GetIssuedToEntityTypeCode(WoApiOrganisation? org)
    {
        switch (org?.Registration.Type)
        {
            case WoApiOrganisationType.ComplianceScheme:
                return OrganisationType.ComplianceScheme_CS;
            case WoApiOrganisationType.LargeProducer:
                return OrganisationType.LargeProducer_DR;
            default:
                logger.LogError(
                    "Unknown registration type {RegistrationType} for organisation {OrganisationId}",
                    org?.Registration.Type,
                    org?.Id
                );
                return null;
        }
    }

    private async Task<WoApiOrganisation?> GetWoApiOrganisation(
        string organisationId,
        CancellationToken cancellationToken
    )
    {
        WoApiOrganisation? org = await HttpHelper.HandleTransientErrorsGet<WoApiOrganisation>(
            async (cancellationToken) =>
                await woService.GetOrganisation(organisationId, cancellationToken),
            logger,
            $"Getting organisation details for {organisationId}",
            cancellationToken
        );
        return org;
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

    private async Task ProcessPrns(
        List<PackagingRecyclingNote> prns,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Processing {Count} prns", prns.Count);
        foreach (var prn in prns)
        {
            var org = await GetWoApiOrganisation(prn.IssuedToOrganisation?.Id!, cancellationToken);
            var request = _mapper.Map<SavePrnDetailsRequest>(prn);
            MapProducerFields(request, org);
            if (await ProcessPrn(request, cancellationToken))
            {
                await SendEmailToProducers(request, org);
            }
        }
    }

    private static void MapProducerFields(SavePrnDetailsRequest request, WoApiOrganisation? org)
    {
        if (org == null)
        {
            return;
        }

        var agency = org.BusinessCountry switch
        {
            WoApiBusinessCountry.England => RpdReprocessorExporterAgency.EnvironmentAgency,
            WoApiBusinessCountry.NorthernIreland =>
                RpdReprocessorExporterAgency.NorthernIrelandEnvironmentAgency,
            WoApiBusinessCountry.Scotland =>
                RpdReprocessorExporterAgency.ScottishEnvironmentProtectionAgency,
            WoApiBusinessCountry.Wales => RpdReprocessorExporterAgency.NaturalResourcesWales,
            _ => null,
        };

        request.PackagingProducer = agency;
        request.ProducerAgency = agency;
    }

    private async Task<bool> ProcessPrn(
        SavePrnDetailsRequest request,
        CancellationToken cancellationToken
    )
    {
        return await HttpHelper.HandleTransientErrors(
            async (ct) => await prnService.SavePrn(request, ct),
            logger,
            $"Saving PRN {request.PrnNumber}",
            cancellationToken
        );
    }
}
