using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.WasteOrganisationsService
{
    [ExcludeFromCodeCoverage(Justification = "This will have test coverage via integration tests.")]
    public class WasteOrganisationsService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<WasteOrganisationsService> logger,
        IOptions<WasteOrganisationsApiConfiguration> config
    )
        : BaseHttpService(
            httpContextAccessor,
            httpClientFactory,
            config.Value.BaseUrl
                ?? throw new ArgumentNullException(
                    nameof(config),
                    ExceptionMessages.WasteOrganisationsApiBaseUrlMissing
                ),
            logger,
            HttpClientNames.WasteOrganisations,
            config.Value.TimeoutSeconds
        ),
            IWasteOrganisationsService
    {
        public async Task<HttpResponseMessage> UpdateOrganisation(
            string id,
            WasteOrganisationsApiUpdateRequest organisation
        )
        {
            return await PutAsync($"organisations/{id}", organisation, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> GetOrganisation(
            string organisationId,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation("Getting organisation details for {OrgId}.", organisationId);
            return await GetAsync($"organisations/{organisationId}", cancellationToken);
        }
    }
}
