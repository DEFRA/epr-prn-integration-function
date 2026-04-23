using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;

namespace EprPrnIntegration.Common.Mappers;

public static class RrepwMappers
{
    public static SavePrnDetailsRequest Map(PackagingRecyclingNote source, Action<string> logWarning)
    {
        return new SavePrnDetailsRequest
        {
            SourceSystemId = source.Id,
            PrnStatusId = ConvertStatusToEprnStatus(source),
            PrnSignatory = GetPrnSignatory(source),
            PrnSignatoryPosition = GetPrnSignatoryPosition(source),
            IssuedByOrg = GetIssuedByOrg(source),
            OrganisationId = GetOrganisationId(source),
            OrganisationName = GetOrganisationName(source, logWarning),
            AccreditationNumber = GetAccreditationNumber(source),
            AccreditationYear = GetAccreditationYear(source),
            ReprocessorExporterAgency = ConvertRegulator(source),
            ReprocessingSite = GetReprocessingSite(source),
            DecemberWaste = source.IsDecemberWaste,
            ProcessToBeUsed = ConvertMaterialToProcessToBeUsed(source),
            ObligationYear = "2026",
            MaterialName = ConvertMaterialToEprnMaterial(source),
            IssueDate = GetAuthorizedAt(source),
            StatusUpdatedOn = GetStatusUpdatedOn(source),
            PrnNumber = source.PrnNumber,
            IsExport = source.IsExport,
            TonnageValue = source.TonnageValue,
            IssuerNotes = source.IssuerNotes
        };
    }

    private static string? GetOrganisationName(PackagingRecyclingNote prn, Action<string> logWarning)
    {
        var registrations = prn.Organisation?.Registrations
            .Where(x =>
                x.Status == WoApiOrganisationStatus.Registered &&
                x.RegistrationYear == prn.Accreditation?.AccreditationYear)
            .ToList() ?? [];

        if (registrations.Exists(x => x.Type == WoApiOrganisationType.ComplianceScheme))
            return UseTradingNameIfPresent(prn);

        if (registrations.Exists(x => x.Type == WoApiOrganisationType.LargeProducer))
            return prn.IssuedToOrganisation?.Name;
        
        logWarning($"Fallback trading name or name mapping for organisation {prn.Organisation?.Id}");
        
        return UseTradingNameIfPresent(prn);
    }

    private static string? UseTradingNameIfPresent(PackagingRecyclingNote source) =>
        string.IsNullOrWhiteSpace(source.IssuedToOrganisation?.TradingName)
            ? source.IssuedToOrganisation?.Name
            : source.IssuedToOrganisation?.TradingName;

    private static DateTime? GetAuthorizedAt(PackagingRecyclingNote prn)
    {
        return prn.Status?.AuthorisedAt;
    }

    private static DateTime? GetStatusUpdatedOn(PackagingRecyclingNote prn)
    {
        return prn.Status?.CurrentStatus switch
        {
            RrepwStatus.Cancelled => prn.Status.CancelledAt,
            RrepwStatus.AwaitingAcceptance => prn.Status.AuthorisedAt,
            _ => null,
        };
    }

    internal static string? GetReprocessingSite(PackagingRecyclingNote src)
    {
        if (src.Accreditation?.SiteAddress == null)
            return null;
        var strings = new List<string?>
        {
            src.Accreditation.SiteAddress.Line1,
            src.Accreditation.SiteAddress.Line2,
            src.Accreditation.SiteAddress.Town,
            src.Accreditation.SiteAddress.County,
            src.Accreditation.SiteAddress.Postcode,
            src.Accreditation.SiteAddress.Country,
        }
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line!.Trim());
        return string.Join(", ", strings);
    }

    private static string? GetAccreditationYear(PackagingRecyclingNote src)
    {
        return src.Accreditation?.AccreditationYear.ToString();
    }

    private static string? GetAccreditationNumber(PackagingRecyclingNote src)
    {
        return src.Accreditation?.AccreditationNumber;
    }

    private static Guid? GetOrganisationId(PackagingRecyclingNote src)
    {
        return string.IsNullOrEmpty(src.IssuedToOrganisation?.Id)
            ? null
            : Guid.Parse(src.IssuedToOrganisation.Id);
    }

    private static string? GetIssuedByOrg(PackagingRecyclingNote src)
    {
        return src.IssuedByOrganisation?.Name;
    }

    private static string? GetPrnSignatoryPosition(PackagingRecyclingNote src)
    {
        return src.Status?.AuthorisedBy?.JobTitle;
    }

    private static string? GetPrnSignatory(PackagingRecyclingNote src)
    {
        return src.Status?.AuthorisedBy?.FullName;
    }

    private static int? ConvertStatusToEprnStatus(PackagingRecyclingNote prn)
    {
        return prn.Status?.CurrentStatus switch
        {
            // only interested in these two, anything else should have been filtered out earlier and so is an error here
            RrepwStatus.AwaitingAcceptance => (int)EprnStatus.AWAITINGACCEPTANCE,
            RrepwStatus.Cancelled => (int)EprnStatus.CANCELLED,
            _ => null,
        };
    }

    private static string? ConvertMaterialToEprnMaterial(PackagingRecyclingNote prn)
    {
        string? material = prn.Accreditation?.Material;
        string? glassRecyclingProcess = prn.Accreditation?.GlassRecyclingProcess;
        return material switch
        {
            RrepwMaterialName.Aluminium => RpdMaterialName.Aluminium,
            RrepwMaterialName.Fibre => RpdMaterialName.Fibre,
            RrepwMaterialName.Glass => glassRecyclingProcess switch
            {
                RrepwGlassRecyclingProcess.GlassOther => RpdMaterialName.GlassOther,
                RrepwGlassRecyclingProcess.GlassRemelt => RpdMaterialName.GlassRemelt,
                _ => null,
            },
            RrepwMaterialName.Paper => RpdMaterialName.PaperBoard,
            RrepwMaterialName.Plastic => RpdMaterialName.Plastic,
            RrepwMaterialName.Steel => RpdMaterialName.Steel,
            RrepwMaterialName.Wood => RpdMaterialName.Wood,
            _ => null,
        };
    }

    private static string? ConvertRegulator(PackagingRecyclingNote src)
    {
        var source = src.Accreditation?.SubmittedToRegulator;
        return source switch
        {
            RrepwSubmittedToRegulator.EnvironmentAgency_EA =>
                RpdReprocessorExporterAgency.EnvironmentAgency,
            RrepwSubmittedToRegulator.NaturalResourcesWales_NRW =>
                RpdReprocessorExporterAgency.NaturalResourcesWales,
            RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_NIEA =>
                RpdReprocessorExporterAgency.NorthernIrelandEnvironmentAgency,
            RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAgency_SEPA =>
                RpdReprocessorExporterAgency.ScottishEnvironmentProtectionAgency,
            _ => null,
        };
    }

    private static string? ConvertMaterialToProcessToBeUsed(PackagingRecyclingNote prn)
    {
        var material = prn.Accreditation?.Material;
        return material switch
        {
            RrepwMaterialName.Aluminium => RpdProcesses.R4,
            RrepwMaterialName.Fibre => RpdProcesses.R3,
            RrepwMaterialName.Glass => RpdProcesses.R5,
            RrepwMaterialName.Paper => RpdProcesses.R3,
            RrepwMaterialName.Plastic => RpdProcesses.R3,
            RrepwMaterialName.Steel => RpdProcesses.R4,
            RrepwMaterialName.Wood => RpdProcesses.R3,
            _ => null,
        };
    }
}
