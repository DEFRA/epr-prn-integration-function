using AutoMapper;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;

namespace EprPrnIntegration.Common.Mappers;

public class RrepwMappers : Profile
{
    public RrepwMappers()
    {
        // all fields required here have been validated as not null prior to this mapping
        CreateMap<PackagingRecyclingNote, SavePrnDetailsRequestV2>()
            .ForMember(spdr => spdr.SourceSystemId, opt => opt.MapFrom(src => src.Id))
            .ForMember(
                spdr => spdr.PrnStatusId,
                o => o.MapFrom(src => ConvertStatusToEprnStatus(src))
            )
            .ForMember(spdr => spdr.PrnSignatory, o => o.MapFrom(src => GetPrnSignatory(src)))
            .ForMember(
                spdr => spdr.PrnSignatoryPosition,
                o => o.MapFrom(src => GetPrnSignatoryPosition(src))
            )
            .ForMember(spdr => spdr.IssuedByOrg, o => o.MapFrom(src => GetIssuedByOrg(src)))
            .ForMember(spdr => spdr.OrganisationId, o => o.MapFrom(src => GetOrganisationId(src)))
            .ForMember(
                spdr => spdr.OrganisationName,
                o => o.MapFrom(src => GetOrganisationName(src))
            )
            .ForMember(
                spdr => spdr.AccreditationNumber,
                o => o.MapFrom(src => GetAccreditationNumber(src))
            )
            .ForMember(
                spdr => spdr.AccreditationYear,
                o => o.MapFrom(src => GetAccreditationYear(src))
            )
            .ForMember(
                spdr => spdr.ReprocessorExporterAgency,
                o => o.MapFrom(src => ConvertRegulator(src))
            )
            .ForMember(
                spdr => spdr.ReprocessingSite,
                o => o.MapFrom(src => GetReprocessingSite(src))
            )
            .ForMember(spdr => spdr.DecemberWaste, o => o.MapFrom(src => src.IsDecemberWaste))
            .ForMember(
                spdr => spdr.ProcessToBeUsed,
                o => o.MapFrom(src => ConvertMaterialToProcessToBeUsed(src))
            )
            .ForMember(spdr => spdr.ObligationYear, o => o.MapFrom(src => "2026"))
            .ForMember(
                spdr => spdr.MaterialName,
                o => o.MapFrom(src => ConvertMaterialToEprnMaterial(src))
            )
            .AfterMap((prn, spdr) => spdr.StatusUpdatedOn = GetStatusUpdatedOn(prn));
    }

    private static DateTime? GetStatusUpdatedOn(PackagingRecyclingNote prn)
    {
        return prn.Status?.CurrentStatus switch
        {
            RrepwStatus.Cancelled => prn.Status.CancelledAt ?? null,
            RrepwStatus.AwaitingAcceptance => prn.Status.AuthorisedAt ?? null,
            _ => null,
        };
    }

    public static string? GetReprocessingSite(PackagingRecyclingNote src)
    {
        if (src.Accreditation?.SiteAddress == null)
            return null;
        return new List<string?>
        {
            src.Accreditation.SiteAddress.Line1,
            src.Accreditation.SiteAddress.Line2,
            src.Accreditation.SiteAddress.Town,
            src.Accreditation.SiteAddress.County,
            src.Accreditation.SiteAddress.Postcode,
            src.Accreditation.SiteAddress.Country,
        }
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Aggregate((current, next) => $"{current}, {next}");
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
        return src.IssuedToOrganisation?.Name;
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
                RpdReprocessorExporterAgency.ScottishEnvironmentProtectionAge,
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
