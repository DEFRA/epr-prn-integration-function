using EprPrnIntegration.Common.Models.Rrepw;

namespace EprPrnIntegration.Common.RESTServices.RrepwPrnService.Interfaces;

public interface IRrepwPrnService
{
    Task<List<PackagingRecyclingNote>> GetPrns(CancellationToken cancellationToken);
}
