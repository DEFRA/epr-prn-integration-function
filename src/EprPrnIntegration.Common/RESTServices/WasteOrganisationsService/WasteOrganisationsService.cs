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
            await Put($"{id}", organisation, CancellationToken.None);
        }
    }
}
