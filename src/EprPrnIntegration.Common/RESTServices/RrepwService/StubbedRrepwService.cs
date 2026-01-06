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
            CancellationToken cancellationToken = default
        )
        {
            logger.LogInformation(
                "Using stubbed RRepw service. Returning mock data for date range {DateFrom} to {DateTo}",
                dateFrom,
                dateTo
            );

            var stubbedData = new List<PackagingRecyclingNote>
            {
                new()
                {
                    Id = "6042083a-fded-4063-91c0-e8c0afcf014e",
                    PrnNumber = "STUB-12345",
                    Status = new Status
                    {
                        CurrentStatus = RrepwStatus.AwaitingAcceptance,
                        AuthorisedAt = DateTime.UtcNow.AddDays(-1),
                    },
                    IssuedByOrganisation = new Organisation
                    {
                        Id = "320353aa-73dd-4f69-aa0c-0905f6027353",
                        Name = "Stubbed Issuer Organisation",
                        TradingName = "Stubbed Issuer Trading",
                    },
                    IssuedToOrganisation = new Organisation
                    {
                        Id = "0b51240c-c013-4973-9d06-d4f90ee4ad8b",
                        Name = "Stubbed Recipient Organisation",
                        TradingName = "Stubbed Recipient Trading",
                    },
                    Accreditation = new Accreditation
                    {
                        Id = "stub-accred-001",
                        AccreditationNumber = "STUB-ACC-001",
                        AccreditationYear = 2026,
                        Material = RrepwMaterialName.Plastic,
                        SubmittedToRegulator = RrepwSubmittedToRegulator.EnvironmentAgency_EA,
                        SiteAddress = new() { Line1 = "123 stub street", Postcode = "ABC 123" },
                    },
                    IsDecemberWaste = false,
                    IsExport = false,
                    TonnageValue = 100,
                    IssuerNotes = "Stubbed packaging recycling note for testing",
                },
            };

            return Task.FromResult(stubbedData);
        }
    }
}
