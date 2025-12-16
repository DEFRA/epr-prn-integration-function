using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

public interface IPrnServiceV2
{
    Task SaveEPRN(Prn request);
}
