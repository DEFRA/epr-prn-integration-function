using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService;

[ExcludeFromCodeCoverage(Justification = "This has integration tests")]
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
        "api/v2",
        logger,
        HttpClientNames.PrnV2,
        config.Value.TimeoutSeconds
    ),
        IPrnService
{
    public async Task SavePrn(SavePrnDetailsRequest request)
    {
        logger.LogInformation("Saving RREPW PRN with id {PrnNumber}", request.PrnNumber);
        await Post("prn", request, CancellationToken.None);
    }

    public async Task<List<PrnUpdateStatus>> GetUpdatedPrns(DateTime fromDate, DateTime toDate)
    {
        logger.LogInformation("Getting updated PRN's.");

        return await Get<List<PrnUpdateStatus>>(
            PrnRoutes.ModifiedPrnsRoute(fromDate, toDate),
            CancellationToken.None,
            false
        );
    }
}
