using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.BackendAccountService
{
    public class OrganisationService : BaseHttpService, IOrganisationService
    {
        private readonly ILogger<OrganisationService> _logger;

        public OrganisationService(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            ILogger<OrganisationService> logger,
            IOptions<Configuration.Service> config)
            : base(httpContextAccessor, httpClientFactory,
               config.Value.Url ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.OrganisationServiceBaseUrlMissing),
               config.Value.EndPointName ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.OrganisationServiceEndPointNameMissing))
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PersonEmail>> GetPersonEmailsAsync(string organisationId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Calling the Get Person Emails Api.");
            return await Get<List<PersonEmail>>($"person-emails?organisationId={organisationId}", cancellationToken, false);
        }
    }
}
