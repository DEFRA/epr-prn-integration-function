using EprPrnIntegration.Api.Models;

namespace EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces
{
    public interface IOrganisationService
    {
        Task<List<PersonEmail>> GetPersonEmailsAsync(string organisationId, CancellationToken cancellationToken);
    }

}
