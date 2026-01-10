using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;

namespace EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces
{
    public interface IRrepwService
    {
        Task<List<PackagingRecyclingNote>> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo
        );
        Task<HttpResponseMessage> UpdatePrn(PrnUpdateStatus prn);
    }
};
