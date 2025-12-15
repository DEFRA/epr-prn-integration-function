using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
        IOptions<WasteOrganisationsApiConfiguration> config)
        : BaseHttpService(httpContextAccessor, httpClientFactory,
            config.Value.BaseUrl ??
            throw new ArgumentNullException(nameof(config), ExceptionMessages.WasteOrganisationsApiBaseUrlMissing),
            "organisations", 
            logger,
            HttpClientNames.WasteOrganisations,
            config.Value.TimeoutSeconds), IWasteOrganisationsService
    {
        private readonly ILogger<WasteOrganisationsService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task UpdateOrganisation(string id, WasteOrganisationsApiUpdateRequest organisation)
        {
            LogPayload(id, organisation);

            await Put($"{id}", organisation, CancellationToken.None);
        }

        // This function is temporary for testing, since we don't yet have a working instance of the service.
        // This MUST be removed before promoting to PRE prod,
        // since it runs the risk of leaking PII with production-like data.
        private void LogPayload(string id, WasteOrganisationsApiUpdateRequest organisation)
        {
            var json = JsonSerializer.Serialize(organisation, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Sending organisation update for {OrganisationId}:\n{Json}", id, json);
        }
    }
}
