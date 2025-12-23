using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

public interface IPrnService
{
    Task SavePrn(SavePrnDetailsRequest request);
}
