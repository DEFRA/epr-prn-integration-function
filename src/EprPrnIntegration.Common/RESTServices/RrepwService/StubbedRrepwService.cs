using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Common.RESTServices.RrepwService
{
    [ExcludeFromCodeCoverage(Justification = "Stub service for testing purposes.")]
    public class StubbedRrepwService(ILogger<StubbedRrepwService> logger) : IRrepwService
    {
        public Task<List<PackagingRecyclingNote>> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Using stubbed RRepw service. Returning mock data for date range {DateFrom} to {DateTo}",
                dateFrom, dateTo);

            var stubbedData = new List<PackagingRecyclingNote>
            {
                new PackagingRecyclingNote
                {
                    Id = "stub-prn-001",
                    PrnNumber = "STUB-12345",
                    Status = new Status
                    {
                        CurrentStatus = RrepwStatus.AwaitingAcceptance,
                        AuthorisedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    IssuedByOrganisation = new Organisation
                    {
                        Id = "stub-org-issuer-001",
                        Name = "Stubbed Issuer Organisation",
                        TradingName = "Stubbed Issuer Trading"
                    },
                    IssuedToOrganisation = new Organisation
                    {
                        Id = "stub-org-recipient-001",
                        Name = "Stubbed Recipient Organisation",
                        TradingName = "Stubbed Recipient Trading"
                    },
                    Accreditation = new Accreditation
                    {
                        Id = "stub-accred-001",
                        AccreditationNumber = "STUB-ACC-001",
                        AccreditationYear = 2026,
                        Material = "Plastic"
                    },
                    IsDecemberWaste = false,
                    IsExport = false,
                    TonnageValue = 100,
                    IssuerNotes = "Stubbed packaging recycling note for testing"
                }
            };

            return Task.FromResult(stubbedData);
        }
    }
}
