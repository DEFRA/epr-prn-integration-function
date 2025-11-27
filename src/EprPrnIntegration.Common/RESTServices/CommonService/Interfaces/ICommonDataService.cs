using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.CommonService.Interfaces
{
    public interface ICommonDataService
    {
        Task<List<UpdatedProducersResponse>> GetUpdatedProducers(DateTime from, DateTime to, CancellationToken cancellationToken);
        Task<List<UpdatedProducersResponseV2>> GetUpdatedProducersV2(DateTime from, DateTime to, CancellationToken cancellationToken);
    }
}
