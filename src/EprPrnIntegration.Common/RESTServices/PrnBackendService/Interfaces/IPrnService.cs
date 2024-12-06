using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;

public interface IPrnService
{
    Task<List<UpdatedPrnsResponseModel>> GetUpdatedPrns(DateTime from, DateTime to,
         CancellationToken cancellationToken);

    Task SavePrn(SavePrnDetailsRequest request);
}