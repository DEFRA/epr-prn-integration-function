using AutoMapper;
using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;

namespace EprPrnIntegration.Common.Mappers;

public class RrepwMappers : Profile
{
    public RrepwMappers()
    {
        // all fields required here have been validated as not null prior to this mapping
        CreateMap<(PackagingRecyclingNote prn, WoApiOrganisation org), SavePrnDetailsRequest>()
            .ForMember(spdr => spdr.SourceSystemId, opt => opt.MapFrom(src => src.prn.Id))
            .ForMember(
                spdr => spdr.PrnStatusId,
                o => o.MapFrom(src => ConvertStatusToEprnStatus(src.prn))
            )
            .ForMember(spdr => spdr.PrnSignatory, o => o.MapFrom(src => GetPrnSignatory(src.prn)))
            .ForMember(
                spdr => spdr.PrnSignatoryPosition,
                o => o.MapFrom(src => GetPrnSignatoryPosition(src.prn))
            )
            .ForMember(spdr => spdr.IssuedByOrg, o => o.MapFrom(src => GetIssuedByOrg(src.prn)))
            .ForMember(
                spdr => spdr.OrganisationId,
                o => o.MapFrom(src => GetOrganisationId(src.prn))
            )
            .ForMember(
                spdr => spdr.OrganisationName,
                o => o.MapFrom(src => GetOrganisationName(src.prn))
            )
            .ForMember(
                spdr => spdr.AccreditationNumber,
                o => o.MapFrom(src => GetAccreditationNumber(src.prn))
            )
            .ForMember(
                spdr => spdr.AccreditationYear,
                o => o.MapFrom(src => GetAccreditationYear(src.prn))
            )
            .ForMember(
                spdr => spdr.ReprocessorExporterAgency,
                o => o.MapFrom(src => ConvertRegulator(src.prn))
            )
            .ForMember(
                spdr => spdr.ReprocessingSite,
                o => o.MapFrom(src => GetReprocessingSite(src.prn))
            )
            .ForMember(spdr => spdr.DecemberWaste, o => o.MapFrom(src => src.prn.IsDecemberWaste))
            .ForMember(
                spdr => spdr.ProcessToBeUsed,
                o => o.MapFrom(src => ConvertMaterialToProcessToBeUsed(src.prn))
            )
            .ForMember(spdr => spdr.ObligationYear, o => o.MapFrom(src => "2026"))
            .ForMember(
                spdr => spdr.MaterialName,
                o => o.MapFrom(src => ConvertMaterialToEprnMaterial(src.prn))
            )
            .ForMember(spdr => spdr.IssueDate, o => o.MapFrom(src => GetAuthorizedAt(src.prn)))
            .ForMember(
                spdr => spdr.PackagingProducer,
                o => o.MapFrom(src => GetProducerField(src.org))
            )
            .ForMember(
                spdr => spdr.ProducerAgency,
                o => o.MapFrom(src => GetProducerField(src.org))
            )
            .AfterMap(
                (src, spdr) =>
                {
                    spdr.StatusUpdatedOn = GetStatusUpdatedOn(src.prn);
                }
            );
    }

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

    public static string? GetReprocessingSite(PackagingRecyclingNote src)
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

    private static string? GetOrganisationName(PackagingRecyclingNote src)
    {
        return string.IsNullOrWhiteSpace(src.IssuedToOrganisation?.TradingName)
            ? src.IssuedToOrganisation?.Name
            : src.IssuedToOrganisation.TradingName;
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

    public static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<RrepwMappers>()).CreateMapper();
    }

    private static EprnStatus? ConvertStatusToEprnStatus(PackagingRecyclingNote prn)
    {
        return prn.Status?.CurrentStatus switch
        {
            // only interested in these two, anything else should have been filtered out earlier and so is an error here
            RrepwStatus.AwaitingAcceptance => EprnStatus.AWAITINGACCEPTANCE,
            RrepwStatus.Cancelled => EprnStatus.CANCELLED,
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
            RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA =>
                RpdReprocessorExporterAgency.NorthernIrelandEnvironmentAgency,
            RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA =>
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

    private static string? GetProducerField(WoApiOrganisation org)
    {
        // TODO: Implement mapping logic for PackagingProducer and ProducerAgency
        return null;
    }
}
