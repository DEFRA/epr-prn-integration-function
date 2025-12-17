using AutoMapper;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;

namespace EprPrnIntegration.Common.Mappers;

public class RrepwMappers : Profile 
{
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RrepwMappers>());
        return config.CreateMapper();
    }

    public RrepwMappers()
    {
        CreateMap<PackagingRecyclingNote, SavePrnDetailsRequestV2>()
            .ForMember(rrepwPrn => rrepwPrn.SourceSystemId, opt => opt.MapFrom(src => src.Id))
            .ForMember(rrepwPrn => rrepwPrn.PrnStatusId, o => o.MapFrom(src => ConvertStatusToEprnStatus( src.Status.CurrentStatus)))
            .ForMember(rrepwPrn => rrepwPrn.PrnSignatory, o => o.MapFrom(src =>  src.Status.AuthorisedBy.FullName))
            .ForMember(rrepwPrn => rrepwPrn.PrnSignatoryPosition, o => o.MapFrom(src =>  src.Status.AuthorisedBy.JobTitle))
            .AfterMap((prn, request) =>
            {
                request.StatusUpdatedOn = prn.Status.CurrentStatus switch
                {
                    "cancelled" =>
                        // todo should not be ignoring this potential null here
                        prn.Status.CancelledAt!.Value,
                    "accepted" => prn.Status.AuthorisedAt!.Value,
                    _ => request.StatusUpdatedOn
                };
            });
    }

    private static EprnStatus ConvertStatusToEprnStatus(string source)
        => source switch
        {
            "awaiting_authorisation" => EprnStatus.AWAITINGACCEPTANCE,
            "awaiting_acceptance"    => EprnStatus.AWAITINGACCEPTANCE,
            "accepted"               => EprnStatus.ACCEPTED,
            "awaiting_cancellation"  => EprnStatus.CANCELLED,
            "cancelled"              => EprnStatus.CANCELLED,
            "rejected"               => EprnStatus.REJECTED,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };

}