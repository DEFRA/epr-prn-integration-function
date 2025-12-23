using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

public interface INpwdPrnService
{
    Task<List<UpdatedNpwdPrnsResponseModel>> GetUpdatedNpwdPrns(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );
    Task InsertPeprNpwdSyncPrns(IEnumerable<UpdatedNpwdPrnsResponseModel> npwdUpdatedPrns);
    Task SaveNpwdPrn(SaveNpwdPrnDetailsRequest request);
    Task<List<ReconcileUpdatedNpwdPrnsResponseModel>> GetReconciledUpdatedNpwdPrns();
}
