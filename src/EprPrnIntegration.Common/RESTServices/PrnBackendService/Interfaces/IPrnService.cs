using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

public interface IPrnService
{
    Task<List<PrnUpdateStatus>> GetUpdatedPrns(DateTime fromDate, DateTime toDate);
    Task SavePrn(SavePrnDetailsRequest request);
}
