using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.RrepwService
{
    [ExcludeFromCodeCoverage(Justification = "This will have test coverage via integration tests.")]
    public class RrepwService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<RrepwService> logger,
        IOptions<RrepwApiConfiguration> config)
        : BaseHttpService(httpContextAccessor, httpClientFactory,
            config.Value.BaseUrl ??
            throw new ArgumentNullException(nameof(config), ExceptionMessages.RrepwApiBaseUrlMissing),
            "v1/packaging-recycling-notes",
            logger,
            HttpClientNames.Rrepw,
            config.Value.TimeoutSeconds), IRrepwService
    {
        public async Task<ListPackagingRecyclingNotesResponse> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default)
        {
            var dateFromQuery = dateFrom.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            var dateToQuery = dateTo.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

            var queryString = $"?statuses=awaiting_acceptance,cancelled&dateFrom={dateFromQuery}&dateTo={dateToQuery}";

            logger.LogInformation("Fetching packaging recycling notes from {DateFrom} to {DateTo}",
                dateFromQuery, dateToQuery);

            return await Get<ListPackagingRecyclingNotesResponse>(queryString, cancellationToken);
        }
    }
}
