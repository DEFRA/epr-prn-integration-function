using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.RrepwPrnService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.RrepwPrnService;

[ExcludeFromCodeCoverage(Justification = "This class is currently just a stub for canned data.")]
public class RrepwPrnService : BaseHttpService, IRrepwPrnService
{
    private readonly ILogger<RrepwPrnService> _logger;

    public RrepwPrnService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<RrepwPrnService> logger,
        IOptions<Configuration.RrepwPrnConfiguration> config)
        : base(httpContextAccessor, httpClientFactory,
            config.Value.BaseUrl ?? throw new ArgumentNullException(nameof(config), "RrepwPrn BaseUrl is missing"),
            "api/prns",
            logger,
            HttpClientNames.RrepwPrn,
            config.Value.TimeoutSeconds)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<NpwdPrn>> GetPrns(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting PRNs from RREPW service (stub implementation)");

        // Stub implementation - return a single test PRN
        await Task.CompletedTask;

        return new List<NpwdPrn>
        {
            new()
            {
                EvidenceNo = "STUB-001",
                AccreditationNo = "ACC-001",
                AccreditationYear = 2025,
                DecemberWaste = false,
                EvidenceMaterial = "Plastic",
                EvidenceStatusCode = "ACTIVE",
                EvidenceStatusDesc = "Active",
                EvidenceTonnes = 100,
                IssueDate = DateTime.UtcNow.AddDays(-30),
                IssuedByNPWDCode = "NPWD-001",
                IssuedByOrgName = "Test Issuer Organization",
                IssuedToNPWDCode = "NPWD-002",
                IssuedToOrgName = "Test Recipient Organization",
                MaterialOperationCode = "MAT-001",
                ModifiedOn = DateTime.UtcNow,
                ObligationYear = 2025,
                RecoveryProcessCode = "REC-001",
                StatusDate = DateTime.UtcNow,
                CreatedByUser = "stub-user",
                IssuedToEntityTypeCode = "CS"
            }
        };
    }
}
