using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

public interface IPrnService
{
    Task<List<UpdatedPrnsResponseModel>> GetUpdatedPrns(DateTime from, DateTime to,
         CancellationToken cancellationToken);
    Task InsertPeprNpwdSyncPrns(IEnumerable<UpdatedPrnsResponseModel> syncedPrns);
    Task SavePrn(SavePrnDetailsRequest request);
    Task<List<ReconcileUpdatedPrnsResponseModel>> GetReconsolidatedUpdatedPrns();
}