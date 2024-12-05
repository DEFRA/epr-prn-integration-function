using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.BackendAccountService;

public class PrnService : BaseHttpService, IPrnService
{
    private readonly ILogger<PrnService> _logger;

    public PrnService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<PrnService> logger,
        IOptions<Configuration.Service> config)
        : base(httpContextAccessor, httpClientFactory,
            config.Value.PrnBaseUrl ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.PrnServiceBaseUrlMissing),
            config.Value.PrnEndPointName ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.PrnServiceEndPointNameMissing))
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<UpdatedPrnsResponseModel>> GetUpdatedPrns(DateTime from, DateTime to,
       CancellationToken cancellationToken)
    {
        var fromDate = from.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        var toDate = to.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        _logger.LogInformation("Getting updated PRN's.");
        return await Get<List<UpdatedPrnsResponseModel>>($"ModifiedPrnsByDate?from={fromDate}&to={toDate}",
            cancellationToken, false);
    }
}