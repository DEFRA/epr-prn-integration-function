using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;

public interface IOrganisationService
{
    Task<List<PersonEmail>> GetPersonEmailsAsync(string organisationId, CancellationToken cancellationToken);

    Task<List<UpdatedProducersResponseModel>> GetUpdatedProducers(DateTime from, DateTime to,
        CancellationToken cancellationToken);
}