using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.BackendAccountService;

public class OrganisationService : BaseHttpServiceOld, IOrganisationService
{
    private readonly ILogger<OrganisationService> _logger;

    public OrganisationService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<OrganisationService> logger,
        IOptions<Configuration.Service> config
    )
        : base(
            httpContextAccessor,
            httpClientFactory,
            config.Value.AccountBaseUrl
                ?? throw new ArgumentNullException(
                    nameof(config),
                    ExceptionMessages.OrganisationServiceBaseUrlMissing
                ),
            config.Value.AccountEndPointName
                ?? throw new ArgumentNullException(
                    nameof(config),
                    ExceptionMessages.OrganisationServiceEndPointNameMissing
                ),
            logger,
            HttpClientNames.Organisation,
            config.Value.TimeoutSeconds
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<PersonEmail>> GetPersonEmailsAsync(
        string organisationId,
        string issuedToEntityTypeCode,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Calling the Get Person Emails Api.");
        return await Get<List<PersonEmail>>(
            $"person-emails?organisationId={organisationId}&entityTypeCode={issuedToEntityTypeCode}",
            cancellationToken,
            false
        );
    }

    public async Task<List<UpdatedProducersResponseModel>> GetUpdatedProducers(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Getting updated producers list.");
        return await Get<List<UpdatedProducersResponseModel>>(
            $"organisation?From={from:yyyy-MM-ddTHH:mm:ss}&To={to:yyyy-MM-ddTHH:mm:ss}",
            cancellationToken,
            false
        );
    }

    /// <inheritdoc/>
    public async Task<bool> DoesProducerOrComplianceSchemeExistAsync(
        string organisationId,
        string entityTypeCode,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Getting organisation exist for {OrgId}.", organisationId);
        return await GetOk(
            $"validate-issued-epr-id?externalId={organisationId}&entityTypeCode={entityTypeCode}",
            cancellationToken,
            false
        );
    }
}
