using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EprPrnIntegration.Common.RESTServices.PrnBackendService;

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
            config.Value.PrnEndPointName ?? throw new ArgumentNullException(nameof(config), ExceptionMessages.PrnServiceEndPointNameMissing), logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<UpdatedPrnsResponseModel>> GetUpdatedPrns(DateTime from, DateTime to,
       CancellationToken cancellationToken)
    {
        var fromDate = from.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        var toDate = to.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        _logger.LogInformation("Getting updated PRN's.");
        return await Get<List<UpdatedPrnsResponseModel>>($"ModifiedPrnsByDate?from={fromDate}&to={toDate}",
            cancellationToken, false);
    }

    public async Task InsertPeprNpwdSyncPrns(IEnumerable<UpdatedPrnsResponseModel> npwdUpdatedPrns)
    {
        _logger.LogInformation("Inserting Sync Prns in common prn backend");
        try
        {
            var syncedPrns = new List<InsertPeprNpwdSyncModel>();
            foreach(var updateprn in  npwdUpdatedPrns)
            {
                syncedPrns.Add((InsertPeprNpwdSyncModel)updateprn);
            }
            await Post("updatesyncstatus", syncedPrns, default);
            _logger.LogInformation("Sync data inserted");
        }
        catch (Exception ex)
        {

            _logger.LogError(ex, "Insert of sync data failed with ex: {exceptionMessage} with sync prns: {npwdUpdatedPrns}"
                , ex.Message,JsonSerializer.Serialize(npwdUpdatedPrns));
        }
        
    }

    public async Task SavePrn(SavePrnDetailsRequest request)
    {
        _logger.LogInformation($"Saving PRN with id {request.EvidenceNo}" );
        await Post($"/prn-details", request, CancellationToken.None);
    }

    public async Task<List<ReconcileUpdatedPrnsResponseModel>> GetReconsolidatedUpdatedPrns()
    {
        var nowDateTime = DateTime.UtcNow;
        var fromDate = nowDateTime.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        var toDate = nowDateTime.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        _logger.LogInformation("Getting reconsolidated updated PRN's.");
        return await Get<List<ReconcileUpdatedPrnsResponseModel>>($"ModifiedPrnsByDate?from={fromDate}&to={toDate}",
            CancellationToken.None, false);
    }
}