using EprPrnIntegration.Common.Models.Rrepw;

namespace EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces
{
    public interface IRrepwService
    {
        Task<ListPackagingRecyclingNotesResponse> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default);
    }
}
