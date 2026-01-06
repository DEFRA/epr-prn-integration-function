using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;

namespace EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces
{
    public interface IRrepwService
    {
        Task<List<PackagingRecyclingNote>> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default
        );
        Task UpdatePrns(List<PrnUpdateStatus> rrepwUpdatedPrns);
    }
}
