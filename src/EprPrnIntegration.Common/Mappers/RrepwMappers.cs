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
        CreateMap<RrepwPrn, SavePrnDetailsRequestV2>()
            .ForMember(rrepwPrn => rrepwPrn.SourceSystemId, opt => opt.MapFrom(src => src.Id))
            .ForMember(rrepwPrn => rrepwPrn.PrnNumber, opt => opt.MapFrom(src => src.PrnNumber))
            .ForMember(rrepwPrn => rrepwPrn.EvidenceStatusCode, o => o.MapFrom(src => ConvertNotStatusToEprnStatus( src.Status.CurrentStatus)))
            .ForMember(rrepwPrn => rrepwPrn.PrnSignatory, o => o.MapFrom(src =>  src.Status.AuthorisedBy.FullName))
            .ForMember(rrepwPrn => rrepwPrn.PrnSignatoryPosition, o => o.MapFrom(src =>  src.Status.AuthorisedBy.JobTitle))
            .AfterMap((prn, request) =>
            {
                switch (prn.Status.CurrentStatus)
                {
                    case NoteStatus.cancelled:
                        request.CancelledDate = prn.Status.CancelledAt;
                        break;
                    case NoteStatus.accepted:
                        request.StatusDate = prn.Status.AuthorisedAt;
                        break;
                    // all other statuses are not relevant so ignored
                }
            });
    }

    private static EprnStatus ConvertNotStatusToEprnStatus(NoteStatus source)
        => source switch
        {
            NoteStatus.awaiting_authorisation => EprnStatus.AWAITINGACCEPTANCE,
            NoteStatus.awaiting_acceptance    => EprnStatus.AWAITINGACCEPTANCE,
            NoteStatus.accepted              => EprnStatus.ACCEPTED,
            NoteStatus.awaiting_cancellation  => EprnStatus.CANCELLED,
            NoteStatus.cancelled             => EprnStatus.CANCELLED,
            NoteStatus.rejected              => EprnStatus.REJECTED,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };

}