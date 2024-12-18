using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Models;

namespace EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;

public interface IOrganisationService
{
    Task<List<PersonEmail>> GetPersonEmailsAsync(string organisationId, CancellationToken cancellationToken);

    Task<List<UpdatedProducersResponseModel>> GetUpdatedProducers(DateTime from, DateTime to,
        CancellationToken cancellationToken);


    /// <summary>
    /// Check if the given organisation id already exists, either for a producer or compliance scheme
    /// </summary>
    /// <param name="organisationId">Uniquely identifies an organisation</param>
    /// <param name="entityTypeCode">Differentiates between producer and compliance scheme</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the direct producer or compliance scheme was found</returns>
    Task<bool> DoesProducerOrComplianceSchemeExistAsync(string organisationId, string entityTypeCode, CancellationToken cancellationToken);
}