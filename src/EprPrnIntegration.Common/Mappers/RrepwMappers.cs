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
            .ForMember(spdr => spdr.PrnStatusId,
                o => o.MapFrom(src => ConvertStatusToEprnStatus(src.Status.CurrentStatus)))
            .ForMember(spdr => spdr.PrnSignatory, o => o.MapFrom(src => src.Status.AuthorisedBy!.FullName))
            .ForMember(spdr => spdr.PrnSignatoryPosition,
                o => o.MapFrom(src => src.Status.AuthorisedBy!.JobTitle))
            .ForMember(spdr => spdr.IssuedByOrg, o => o.MapFrom(src => src.IssuedByOrganisation.Name))
            .ForMember(spdr => spdr.OrganisationId,
                o => o.MapFrom(src => Guid.Parse(src.IssuedToOrganisation.Id)))
            .ForMember(spdr => spdr.OrganisationName, o => o.MapFrom(src => src.IssuedToOrganisation.Name))
            .ForMember(spdr => spdr.OrganisationName, o => o.MapFrom(src => src.IssuedToOrganisation.Name))
            .ForMember(spdr => spdr.AccreditationNumber,
                o => o.MapFrom(src => src.Accreditation.AccreditationNumber))
            .ForMember(spdr => spdr.AccreditationYear,
                o => o.MapFrom(src => src.Accreditation.AccreditationYear.ToString()))
            .ForMember(spdr => spdr.ReprocessorExporterAgency,
                o => o.MapFrom(src => ConvertRegulator( src.Accreditation.SubmittedToRegulator)))
            .ForMember(spdr => spdr.ReprocessingSite,
                o => o.MapFrom(src => src.Accreditation.SiteAddress!.Line1))
            .ForMember(spdr => spdr.DecemberWaste, o => o.MapFrom(src => src.IsDecemberWaste))
            .ForMember(spdr => spdr.ProcessToBeUsed,
                o => o.MapFrom(src => ConvertMaterialToProcessToBeUsed(src.Accreditation.Material)))
            .ForMember(spdr => spdr.ObligationYear,o => o.MapFrom(src => "2026"))
            .ForMember(spdr => spdr.MaterialName,
                o => o.MapFrom(src => ConvertMaterialToEprnMaterial(src.Accreditation.Material, src.Accreditation.GlassRecyclingProcess)))
            .AfterMap((prn, spdr) =>
            {
                spdr.StatusUpdatedOn = prn.Status.CurrentStatus switch
                {
                    StatusName.Cancelled =>
                        prn.Status.CancelledAt!.Value,
                    StatusName.AwaitingAcceptance => prn.Status.AuthorisedAt!.Value,
                    _ => spdr.StatusUpdatedOn
                };
            });
    }

    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RrepwMappers>());
        return config.CreateMapper();
    }

    private static EprnStatus ConvertStatusToEprnStatus(string source)
    {
        return source switch
        {
            StatusName.AwaitingAuthorisation => EprnStatus.AWAITINGACCEPTANCE,
            StatusName.AwaitingAcceptance => EprnStatus.AWAITINGACCEPTANCE,
            StatusName.Accepted => EprnStatus.ACCEPTED,
            StatusName.AwaitingCancellation => EprnStatus.CANCELLED,
            StatusName.Cancelled => EprnStatus.CANCELLED,
            StatusName.Rejected => EprnStatus.REJECTED,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
    private static string ConvertMaterialToEprnMaterial(string material, string? glassRecyclingProcess)
    {
        return material switch
        {
            RrepwMaterialName.Aluminium => RpdMaterialName.Aluminium,
            RrepwMaterialName.Fibre => RpdMaterialName.Fibre,
            RrepwMaterialName.Glass => glassRecyclingProcess switch
            {
                RrepwGlassRecyclingProcess.GlassOther => RpdMaterialName.GlassOther,
                RrepwGlassRecyclingProcess.GlassRemelt => RpdMaterialName.GlassRemelt,
                _ => throw new ArgumentOutOfRangeException(nameof(glassRecyclingProcess), glassRecyclingProcess, null)
            },
            RrepwMaterialName.Paper => RpdMaterialName.PaperBoard,
            RrepwMaterialName.Plastic => RpdMaterialName.Plastic,
            RrepwMaterialName.Steel => RpdMaterialName.Steel,
            RrepwMaterialName.Wood => RpdMaterialName.Wood,
            _ => throw new ArgumentOutOfRangeException(nameof(material), material, null)
        };
    }
    
    
    private static string ConvertRegulator(string source)
    {
        return source switch
        {
            RrepwSubmittedToRegulator.EnvironmentAgency => RpdSubmittedToRegulator.EnvironmentAgency,
            RrepwSubmittedToRegulator.NaturalResourcesWales => RpdSubmittedToRegulator.NaturalResourcesWales,
            RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency => RpdSubmittedToRegulator.NorthernIrelandEnvironmentAgency,
            RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge => RpdSubmittedToRegulator.ScottishEnvironmentProtectionAge,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
    
    private static string ConvertMaterialToProcessToBeUsed(string material)
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
            _ => throw new ArgumentOutOfRangeException(nameof(material), material, null)
        };
    }
}
