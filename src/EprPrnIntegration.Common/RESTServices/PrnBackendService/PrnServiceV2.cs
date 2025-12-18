using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService;

public class PrnServiceV2 : BaseHttpService, IPrnServiceV2
{
    private readonly ILogger<PrnServiceV2> _logger;

    public PrnServiceV2(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<PrnServiceV2> logger,
        IOptions<Configuration.Service> config)
        : base(httpContextAccessor, httpClientFactory,
            config.Value.PrnBaseUrl ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.PrnServiceBaseUrlMissing),
            config.Value.PrnEndPointNameV2 ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.PrnServiceEndPointNameV2Missing),
            logger,
            HttpClientNames.PrnV2,
            config.Value.TimeoutSeconds)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SavePrn(SavePrnDetailsRequestV2 request)
    {
        _logger.LogInformation("Saving PRN V2 with id {PrnNumber}", request.PrnNumber);
        await Post($"/", request, CancellationToken.None);
    }
}
