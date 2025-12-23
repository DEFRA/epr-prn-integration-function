using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService;

public class PrnService(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    ILogger<PrnService> logger,
    IOptions<Configuration.Service> config
)
    : BaseHttpService(
        httpContextAccessor,
        httpClientFactory,
        config.Value.PrnBaseUrl
            ?? throw new ArgumentNullException(
                nameof(config),
                ExceptionMessages.PrnServiceBaseUrlMissing
            ),
        config.Value.PrnEndPointNameV2
            ?? throw new ArgumentNullException(
                nameof(config),
                ExceptionMessages.PrnServiceEndPointNameV2Missing
            ),
        logger,
        HttpClientNames.PrnV2,
        config.Value.TimeoutSeconds
    ),
        IPrnService
{
    public async Task SavePrn(SavePrnDetailsRequest request)
    {
        logger.LogInformation("Saving RREPW PRN with id {PrnNumber}", request.PrnNumber);
        await Post($"/", request, CancellationToken.None);
    }

    public async Task<List<PrnUpdateStatus>> GetUpdatedPrns(DateTime from, DateTime to)
    {
        var fromDate = from.ToString(
            "yyyy-MM-ddTHH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture
        );
        var toDate = to.ToString(
            "yyyy-MM-ddTHH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture
        );
        logger.LogInformation("Getting updated PRN's.");
        var uriBuilder = new UriBuilder(new Uri("modified-prns", UriKind.Relative))
        {
            Query = $"from={fromDate}&to={toDate}",
        };
        return await Get<List<PrnUpdateStatus>>(
            uriBuilder.ToString(),
            CancellationToken.None,
            false
        );
    }
}
