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
                o =>
                    o.MapFrom(src =>
                        ConvertStatusToEprnStatus(
                            src.Status == null ? null : src.Status.CurrentStatus
                        )
                    )
            )
            .ForMember(
                spdr => spdr.PrnSignatory,
                o =>
                    o.MapFrom(src =>
                        src.Status == null || src.Status.AuthorisedBy == null
                            ? null
                            : src.Status.AuthorisedBy.FullName
                    )
            )
            .ForMember(
                spdr => spdr.PrnSignatoryPosition,
                o =>
                    o.MapFrom(src =>
                        src.Status == null || src.Status.AuthorisedBy == null
                            ? null
                            : src.Status.AuthorisedBy.JobTitle
                    )
            )
            .ForMember(
                spdr => spdr.IssuedByOrg,
                o =>
                    o.MapFrom(src =>
                        src.IssuedByOrganisation == null ? null : src.IssuedByOrganisation.Name
                    )
            )
            .ForMember(
                spdr => spdr.OrganisationId,
                o =>
                    o.MapFrom<Guid?>(src =>
                        src.IssuedToOrganisation == null
                        || string.IsNullOrEmpty(src.IssuedToOrganisation.Id)
                            ? null
                            : Guid.Parse(src.IssuedToOrganisation.Id)
                    )
            )
            .ForMember(
                spdr => spdr.OrganisationName,
                o =>
                    o.MapFrom(src =>
                        src.IssuedToOrganisation == null ? null : src.IssuedToOrganisation.Name
                    )
            )
            .ForMember(
                spdr => spdr.AccreditationNumber,
                o =>
                    o.MapFrom(src =>
                        src.Accreditation == null ? null : src.Accreditation.AccreditationNumber
                    )
            )
            .ForMember(
                spdr => spdr.AccreditationYear,
                o =>
                    o.MapFrom(src =>
                        src.Accreditation == null
                            ? null
                            : src.Accreditation.AccreditationYear.ToString()
                    )
            )
            .ForMember(
                spdr => spdr.ReprocessorExporterAgency,
                o =>
                    o.MapFrom(src =>
                        ConvertRegulator(
                            src.Accreditation == null
                            || src.Accreditation.SubmittedToRegulator == null
                                ? null
                                : src.Accreditation.SubmittedToRegulator
                        )
                    )
            )
            .ForMember(
                spdr => spdr.ReprocessingSite,
                o =>
                    o.MapFrom(src =>
                        src.Accreditation == null || src.Accreditation.SiteAddress == null
                            ? null
                            : src.Accreditation.SiteAddress.Line1
                    )
            )
            .ForMember(spdr => spdr.DecemberWaste, o => o.MapFrom(src => src.IsDecemberWaste))
            .ForMember(
                spdr => spdr.ProcessToBeUsed,
                o =>
                    o.MapFrom(src =>
                        ConvertMaterialToProcessToBeUsed(
                            src.Accreditation == null ? null : src.Accreditation.Material
                        )
                    )
            )
            .ForMember(spdr => spdr.ObligationYear, o => o.MapFrom(src => "2026"))
            .ForMember(
                spdr => spdr.MaterialName,
                o =>
                    o.MapFrom(src =>
                        src.Accreditation == null
                            ? null
                            : ConvertMaterialToEprnMaterial(
                                src.Accreditation.Material,
                                src.Accreditation == null
                                    ? null
                                    : src.Accreditation.GlassRecyclingProcess
                            )
                    )
            )
            .AfterMap(
                (prn, spdr) =>
                {
                    spdr.StatusUpdatedOn = prn.Status?.CurrentStatus switch
                    {
                        StatusName.Cancelled => prn.Status.CancelledAt ?? null,
                        StatusName.AwaitingAcceptance => prn.Status.AuthorisedAt ?? null,
                        _ => null,
                    };
                }
            );
    }

    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RrepwMappers>());
        return config.CreateMapper();
    }

    private static EprnStatus? ConvertStatusToEprnStatus(string? source)
    {
        return source switch
        {
            // only interested in these two, anything else should have been filtered out earlier and so is an error here
            StatusName.AwaitingAcceptance => EprnStatus.AWAITINGACCEPTANCE,
            StatusName.Cancelled => EprnStatus.CANCELLED,
            _ => null,
        };
    }

    private static string? ConvertMaterialToEprnMaterial(
        string? material,
        string? glassRecyclingProcess
    )
    {
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

    private static string? ConvertRegulator(string? source)
    {
        return source switch
        {
            RrepwSubmittedToRegulator.EnvironmentAgency_EA =>
                RpdSubmittedToRegulator.EnvironmentAgency,
            RrepwSubmittedToRegulator.NaturalResourcesWales_NRW =>
                RpdSubmittedToRegulator.NaturalResourcesWales,
            RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA =>
                RpdSubmittedToRegulator.NorthernIrelandEnvironmentAgency,
            RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA =>
                RpdSubmittedToRegulator.ScottishEnvironmentProtectionAge,
            _ => null,
        };
    }

    private static string? ConvertMaterialToProcessToBeUsed(string? material)
    {
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
