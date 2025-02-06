using EprPrnIntegration.Common.Models.Npwd;

namespace EprPrnIntegration.Common.Service;

public interface IAppInsightsService
{
    Task<List<ReconcileIssuedPrn>> GetIssuedPrnCustomEventLogsLast24hrsAsync();
    Task<List<UpdatedOrganisationReconciliationSummary>> GetUpdatedOrganisationsCustomEventLogsLast24hrsAsync();
}
