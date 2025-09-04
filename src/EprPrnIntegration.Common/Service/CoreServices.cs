using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

namespace EprPrnIntegration.Common.Service;

[ExcludeFromCodeCoverage]
public sealed class CoreServices
{
    public INpwdClient NpwdClient { get; }
    public IOrganisationService OrganisationService { get; }
    public IPrnService PrnService { get; }

    public CoreServices(
        INpwdClient npwdClient,
        IOrganisationService organisationService,
        IPrnService prnService)
    {
        NpwdClient = npwdClient;
        OrganisationService = organisationService;
        PrnService = prnService;
    }
}
