using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.RrepwPrnService.Interfaces;

public interface IRrepwPrnService
{
    Task<List<NpwdPrn>> GetPrns(CancellationToken cancellationToken);
}
