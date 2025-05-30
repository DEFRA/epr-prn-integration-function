﻿using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.CommonService
{
    public class CommonDataService : BaseHttpService, ICommonDataService
    {
        private readonly ILogger<CommonDataService> _logger;

        public CommonDataService(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            ILogger<CommonDataService> logger,
            IOptions<Configuration.Service> config)
            : base(httpContextAccessor, httpClientFactory,
                config.Value.CommonDataServiceBaseUrl ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.CommonDataServiceBaseUrlMissing),
                config.Value.CommonDataServiceEndPointName ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.CommonDataServiceEndPointNameMissing), logger, HttpClientNames.CommonData,
                config.Value.TimeoutSeconds)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<List<UpdatedProducersResponse>> GetUpdatedProducers(DateTime from, DateTime to,
            CancellationToken cancellationToken)
        {
            var fromDate = from.ToString("yyyy-MM-ddTHH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            var toDate = to.ToString("yyyy-MM-ddTHH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            _logger.LogInformation("Getting updated producers list.");
            return await Get<List<UpdatedProducersResponse>>($"get-updated-producers?from={fromDate}&to={toDate}",
                cancellationToken, false);
        }
    }
}