using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

namespace EprPrnIntegration.Common.Service;

public interface ICoreServices
{
    INpwdClient NpwdClient { get; }
    IOrganisationService OrganisationService { get; }
    INpwdPrnService PrnService { get; }
}
