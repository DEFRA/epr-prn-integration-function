using System.Diagnostics.CodeAnalysis;
using System.Net;
using AutoMapper;
using EprPrnIntegration.Api.Services;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Api.Functions;

[SuppressMessage(
    "SonarQube",
    "S107:Methods should not have too many parameters",
    Justification = "Dependencies injected via primary constructor"
)]
public class FetchRrepwIssuedPrnsFunction(
    ILastUpdateService lastUpdateService,
    ILogger<FetchRrepwIssuedPrnsFunction> logger,
    IRrepwService rrepwService,
    IPrnService prnService,
    IOptions<FetchRrepwIssuedPrnsConfiguration> config,
    IWasteOrganisationsService woService,
    IProducerEmailService producerEmailService,
    IUtilities utilities
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
        await producerEmailService.SendEmailToProducersAsync(request, woOrganisation);
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

    private async Task ProcessPrns(
        List<PackagingRecyclingNote> prns,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Processing {Count} prns", prns.Count);
        foreach (var prn in prns)
        {
            var org = await GetWoApiOrganisation(prn.IssuedToOrganisation?.Id!, cancellationToken);
            if (org == null)
                continue;
            var request = _mapper.Map<SavePrnDetailsRequest>(prn);
            MapProducerFields(request, org);
            if (await ProcessPrn(request, cancellationToken))
            {
                LogCustomEvents(prn);
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
            shouldNotContinueOn: [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound],
            cancellationToken
        );
    }

    private void LogCustomEvents(PackagingRecyclingNote rrepwIssuedPrn)
    {
        Dictionary<string, string> eventData = new()
        {
            { CustomEventFields.PrnNumber, rrepwIssuedPrn.PrnNumber ?? "No PRN Number" },
            {
                CustomEventFields.IncomingStatus,
                rrepwIssuedPrn.Status?.CurrentStatus ?? "Blank Incoming Status"
            },
            { CustomEventFields.Date, DateTime.UtcNow.ToString() },
            {
                CustomEventFields.OrganisationName,
                rrepwIssuedPrn.IssuedToOrganisation?.Name ?? "Blank Organisation Name"
            },
        };

        utilities.AddCustomEvent(CustomEvents.InsertPrnFromRrepw, eventData);
    }
}
