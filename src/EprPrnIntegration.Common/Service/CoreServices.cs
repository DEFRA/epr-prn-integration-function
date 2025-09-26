using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;

namespace EprPrnIntegration.Common.Service;

[ExcludeFromCodeCoverage]
public sealed class CoreServices : ICoreServices
{
    public INpwdClient NpwdClient { get; }
    public IOrganisationService OrganisationService { get; }
    public IPrnService PrnService { get; }

    public CoreServices(
        INpwdClient npwdClient,
        IOrganisationService organisationService,
        IPrnService prnService)
    {
        NpwdClient = npwdClient ?? throw new ArgumentNullException(nameof(npwdClient));
        OrganisationService = organisationService ?? throw new ArgumentNullException(nameof(organisationService));
        PrnService = prnService ?? throw new ArgumentNullException(nameof(prnService));
    }
}
