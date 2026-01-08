using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

public interface IPrnService
{
    Task<HttpResponseMessage> GetUpdatedPrns(DateTime fromDate, DateTime toDate);
    Task<HttpResponseMessage> SavePrn(SavePrnDetailsRequest request);
}
