using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models.Rrepw;
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

    public async Task<List<PackagingRecyclingNote>> GetPrns(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting PRNs from RREPW service (stub implementation)");

        // Stub implementation - return a single test PRN
        await Task.CompletedTask;

        return new List<PackagingRecyclingNote>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                PrnNumber = "STUB-001",
                Status = new Status
                {
                    CurrentStatus = "ACTIVE",
                    AuthorisedAt = DateTime.UtcNow.AddDays(-30)
                },
                IssuedByOrganisation = new Organisation
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Test Issuer Organization"
                },
                IssuedToOrganisation = new Organisation
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Test Recipient Organization"
                },
                Accreditation = new Accreditation
                {
                    Id = Guid.NewGuid().ToString(),
                    AccreditationNumber = "ACC-001",
                    AccreditationYear = 2025,
                    Material = "Plastic",
                    SubmittedToRegulator = "EA"
                },
                IsDecemberWaste = false,
                IsExport = false,
                TonnageValue = 100,
                IssuerNotes = "Stub test PRN"
            }
        };
    }
}
