using System.Diagnostics.CodeAnalysis;
using System.Net;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.RrepwService
{
    [ExcludeFromCodeCoverage(Justification = "Stub service for testing purposes.")]
    public class StubbedRrepwService(
        ILogger<StubbedRrepwService> logger,
        IOptions<RrepwApiConfiguration> rrepwApiConfig
    ) : IRrepwService
    {
        private const string Prn13FixedId = "prn13-fixed-id-for-update-test";

        public Task<List<PackagingRecyclingNote>> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo
        )
        {
            logger.LogInformation(
                "Using stubbed RRepw service. Returning mock data for date range {DateFrom} to {DateTo}",
                dateFrom,
                dateTo
            );

            var now = DateTime.UtcNow;
            var hourlyPrnSuffix = now.ToString("yyyyMMddHH");
            var stubOrgId = rrepwApiConfig.Value.StubOrgId;

            var stubbedData = new List<PackagingRecyclingNote>
            {
                CreatePrn(
                    new PrnScenario("01", hourlyPrnSuffix, stubOrgId, 100),
                    CreateAwaitingAcceptanceStatus("01", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "01",
                        RrepwMaterialName.Plastic,
                        new AccreditationOptions(IncludeFullAddress: true)
                    )
                ),
                CreatePrn(
                    new PrnScenario("02", hourlyPrnSuffix, stubOrgId, 150, IsExport: true),
                    CreateAwaitingAcceptanceStatus("02", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "02",
                        RrepwMaterialName.Plastic,
                        new AccreditationOptions(IncludeSiteAddress: false)
                    )
                ),
                CreatePrn(
                    new PrnScenario(
                        "03",
                        hourlyPrnSuffix,
                        stubOrgId,
                        200,
                        IssuerNotes: "Stubbed cancelled PRN-03 for AC6 testing"
                    ),
                    CreateCancelledStatus("03", dateTo.AddMinutes(-1), dateFrom.AddMinutes(1)),
                    CreateAccreditation("03", RrepwMaterialName.Plastic)
                ),
                CreatePrn(
                    new PrnScenario(
                        "04",
                        hourlyPrnSuffix,
                        stubOrgId,
                        250,
                        IsExport: true,
                        IssuerNotes: "Stubbed cancelled PRN-04 for AC6 testing"
                    ),
                    CreateCancelledStatus("04", dateTo.AddMinutes(-1), dateFrom.AddMinutes(1)),
                    CreateAccreditation("04", RrepwMaterialName.Plastic)
                ),
                CreatePrn(
                    new PrnScenario(
                        "05",
                        hourlyPrnSuffix,
                        stubOrgId,
                        300,
                        IncludeIssuedByTradingName: false,
                        TradingName: $"Stubbed Recipient Trading 05",
                        IssuerNotes: "Stubbed rejected PRN-05 - should NOT be persisted (AC2)"
                    ),
                    CreateRejectedStatus(dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "05",
                        RrepwMaterialName.Plastic,
                        new AccreditationOptions(IncludeSiteAddress: false)
                    )
                ),
                CreatePrn(
                    new PrnScenario("06", hourlyPrnSuffix, stubOrgId, 350),
                    CreateAwaitingAcceptanceStatus("06", dateFrom.AddMinutes(-1)),
                    CreateAccreditation(
                        "06",
                        RrepwMaterialName.Paper,
                        new AccreditationOptions(IncludeFullAddress: true)
                    )
                ),
                CreatePrn(
                    new PrnScenario("07", hourlyPrnSuffix, stubOrgId, 400),
                    CreateAwaitingAcceptanceStatus("07", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "07",
                        RrepwMaterialName.Aluminium,
                        new AccreditationOptions(
                            Regulator: RrepwSubmittedToRegulator.NaturalResourcesWales_NRW,
                            IncludeFullAddress: true
                        )
                    )
                ),
                CreatePrn(
                    new PrnScenario(
                        "08",
                        hourlyPrnSuffix,
                        stubOrgId,
                        450,
                        IssuerNotes: $"Stubbed glass PRN-08 for AC8 testing ({RrepwGlassRecyclingProcess.GlassRemelt})"
                    ),
                    CreateAwaitingAcceptanceStatus("08", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "08",
                        RrepwMaterialName.Glass,
                        new AccreditationOptions(
                            GlassRecyclingProcess: RrepwGlassRecyclingProcess.GlassRemelt,
                            SiteAddress: CreateSiteAddress("08", "Glass")
                        )
                    )
                ),
                CreatePrn(
                    new PrnScenario(
                        "09",
                        hourlyPrnSuffix,
                        stubOrgId,
                        500,
                        IssuerNotes: $"Stubbed glass PRN-09 for AC8 testing ({RrepwGlassRecyclingProcess.GlassOther})"
                    ),
                    CreateAwaitingAcceptanceStatus("09", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "09",
                        RrepwMaterialName.Glass,
                        new AccreditationOptions(
                            GlassRecyclingProcess: RrepwGlassRecyclingProcess.GlassOther,
                            SiteAddress: CreateSiteAddress("09", "Glass")
                        )
                    )
                ),
                CreatePrn(
                    new PrnScenario("10", hourlyPrnSuffix, stubOrgId, 550),
                    CreateAwaitingAcceptanceStatus("10", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "10",
                        RrepwMaterialName.Steel,
                        new AccreditationOptions(
                            Regulator: RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA,
                            IncludeFullAddress: true
                        )
                    )
                ),
                CreatePrn(
                    new PrnScenario("11", hourlyPrnSuffix, stubOrgId, 600),
                    CreateAwaitingAcceptanceStatus("11", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "11",
                        RrepwMaterialName.Fibre,
                        new AccreditationOptions(
                            Regulator: RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA,
                            IncludeFullAddress: true
                        )
                    )
                ),
                CreatePrn(
                    new PrnScenario(
                        "12",
                        hourlyPrnSuffix,
                        stubOrgId,
                        650,
                        IsDecemberWaste: true,
                        IssuerNotes: "Stubbed PRN-12 with ignored fields for AC4 testing"
                    ),
                    CreateAwaitingAcceptanceStatusWithIgnoredFields("12", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "12",
                        RrepwMaterialName.Wood,
                        new AccreditationOptions(
                            SiteAddress: CreateSiteAddress("12", "Wood", includeFullAddress: true)
                        )
                    )
                ),
                CreateUpdateTestPrn(
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    authorisedAt: dateTo.AddMinutes(-1),
                    stubOrgId: stubOrgId,
                    tonnageValue: 700 + now.Minute
                ),
                CreatePrn(
                    new PrnScenario("14", hourlyPrnSuffix, stubOrgId, 800),
                    CreateAwaitingAcceptanceStatus("14", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "14",
                        RrepwMaterialName.Plastic,
                        new AccreditationOptions(AccreditationYear: 2026, IncludeFullAddress: true)
                    )
                ),
                CreatePrn(
                    new PrnScenario(
                        "15",
                        hourlyPrnSuffix,
                        stubOrgId,
                        800,
                        OrgName: "ABC Scheme Operator Ltd",
                        TradingName: "ABC Packaging Scheme"
                    ),
                    CreateAwaitingAcceptanceStatus("15", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "15",
                        RrepwMaterialName.Plastic,
                        new AccreditationOptions(IncludeFullAddress: true)
                    )
                ),
                CreatePrn(
                    new PrnScenario(
                        "16",
                        hourlyPrnSuffix,
                        stubOrgId,
                        800,
                        OrgName: "XYZ Scheme Operator Ltd"
                    ),
                    CreateAwaitingAcceptanceStatus("16", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "16",
                        RrepwMaterialName.Plastic,
                        new AccreditationOptions(IncludeFullAddress: true)
                    )
                ),
                CreatePrn(
                    new PrnScenario(
                        "17",
                        hourlyPrnSuffix,
                        stubOrgId,
                        800,
                        OrgName: "Producer Company Ltd"
                    ),
                    CreateAwaitingAcceptanceStatus("17", dateFrom.AddMinutes(1)),
                    CreateAccreditation(
                        "17",
                        RrepwMaterialName.Plastic,
                        new AccreditationOptions(IncludeFullAddress: true)
                    )
                ),
            };

            logger.LogInformation(
                "Stubbed RRepw service returning {Count} PRNs for AC1-AC12 validation",
                stubbedData.Count
            );

            return Task.FromResult(stubbedData);
        }

        public Task<HttpResponseMessage> UpdatePrn(PrnUpdateStatus prn)
        {
            logger.LogInformation(
                "Using stubbed RRepw service. UpdatePrn called with prn {PrnNumber}, date {StatusDate} and SourceSystemId {SourceSystemId}",
                prn.PrnNumber,
                prn.StatusDate,
                prn.SourceSystemId
            );
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }

        #region Parameter Records

        private sealed record PrnScenario(
            string ScenarioId,
            string HourlyPrnSuffix,
            string StubOrgId,
            int TonnageValue,
            bool IsExport = false,
            bool IsDecemberWaste = false,
            bool IncludeIssuedByTradingName = true,
            string? TradingName = null,
            string? IssuerNotes = null,
            string? OrgName = null
        );

        private sealed record AccreditationOptions(
            string Regulator = RrepwSubmittedToRegulator.EnvironmentAgency_EA,
            int AccreditationYear = 2026,
            Address? SiteAddress = null,
            string? GlassRecyclingProcess = null,
            bool IncludeSiteAddress = true,
            bool IncludeFullAddress = false
        );

        #endregion

        #region PRN Factory Methods

        private static PackagingRecyclingNote CreatePrn(
            PrnScenario scenario,
            Status status,
            Accreditation accreditation
        )
        {
            return new PackagingRecyclingNote
            {
                Id = $"stub-prn-{scenario.ScenarioId}-{Guid.NewGuid()}",
                PrnNumber = $"STUB-PRN{scenario.ScenarioId}-{scenario.HourlyPrnSuffix}",
                Status = status,
                IssuedByOrganisation = CreateIssuedByOrganisation(
                    scenario.ScenarioId,
                    scenario.IncludeIssuedByTradingName
                ),
                IssuedToOrganisation = CreateIssuedToOrganisation(
                    scenario.ScenarioId,
                    scenario.StubOrgId,
                    scenario.OrgName,
                    scenario.TradingName
                ),
                Accreditation = accreditation,
                IsDecemberWaste = scenario.IsDecemberWaste,
                IsExport = scenario.IsExport,
                TonnageValue = scenario.TonnageValue,
                IssuerNotes =
                    scenario.IssuerNotes ?? $"Stubbed PRN-{scenario.ScenarioId} for AC testing",
            };
        }

        private static PackagingRecyclingNote CreateUpdateTestPrn(
            string hourlyPrnSuffix,
            DateTime authorisedAt,
            string stubOrgId,
            int tonnageValue
        )
        {
            return new PackagingRecyclingNote
            {
                Id = Prn13FixedId,
                PrnNumber = $"STUB-PRN13-{hourlyPrnSuffix}",
                Status = CreateAwaitingAcceptanceStatus("13", authorisedAt),
                IssuedByOrganisation = CreateIssuedByOrganisation("13"),
                IssuedToOrganisation = CreateIssuedToOrganisation("13", stubOrgId),
                Accreditation = CreateAccreditation("13", RrepwMaterialName.Plastic),
                IsDecemberWaste = false,
                IsExport = false,
                TonnageValue = tonnageValue,
                IssuerNotes = $"Stubbed PRN-13 for AC3 update testing - tonnage: {tonnageValue}",
            };
        }

        #endregion

        #region Status Factory Methods

        private static Status CreateAwaitingAcceptanceStatus(
            string scenarioId,
            DateTime authorisedAt
        )
        {
            return new Status
            {
                CurrentStatus = RrepwStatus.AwaitingAcceptance,
                AuthorisedAt = authorisedAt,
                AuthorisedBy = CreateAuthorisedBy(scenarioId),
            };
        }

        private static Status CreateAwaitingAcceptanceStatusWithIgnoredFields(
            string scenarioId,
            DateTime authorisedAt
        )
        {
            return new Status
            {
                CurrentStatus = RrepwStatus.AwaitingAcceptance,
                AuthorisedAt = authorisedAt,
                AuthorisedBy = CreateAuthorisedBy(scenarioId),
                AcceptedAt = DateTime.UtcNow.AddDays(-5),
            };
        }

        private static Status CreateCancelledStatus(
            string scenarioId,
            DateTime cancelledAt,
            DateTime authorisedAt
        )
        {
            return new Status
            {
                CurrentStatus = RrepwStatus.Cancelled,
                CancelledAt = cancelledAt,
                AuthorisedAt = authorisedAt,
                AuthorisedBy = CreateAuthorisedBy(scenarioId),
            };
        }

        private static Status CreateRejectedStatus(DateTime rejectedAt)
        {
            return new Status { CurrentStatus = RrepwStatus.Rejected, RejectedAt = rejectedAt };
        }

        private static UserSummary CreateAuthorisedBy(string scenarioId)
        {
            return new UserSummary
            {
                FullName = $"Stub Signatory {scenarioId}",
                JobTitle = "Test Manager",
            };
        }

        #endregion

        #region Organisation Factory Methods

        private static Organisation CreateIssuedByOrganisation(
            string scenarioId,
            bool includeTradingName = true
        )
        {
            return new Organisation
            {
                Id = $"issuer-org-{scenarioId}",
                Name = $"Stubbed Issuer Organisation {scenarioId}",
                TradingName = includeTradingName ? $"Stubbed Issuer Trading {scenarioId}" : null,
            };
        }

        private static Organisation CreateIssuedToOrganisation(
            string scenarioId,
            string stubOrgId,
            string? orgName = null,
            string? tradingName = null
        )
        {
            return new Organisation
            {
                Id = stubOrgId,
                Name = orgName ?? $"Stubbed Recipient Organisation {scenarioId}",
                TradingName = tradingName,
            };
        }

        #endregion

        #region Accreditation Factory Methods

        private static Accreditation CreateAccreditation(
            string scenarioId,
            string material,
            AccreditationOptions? options = null
        )
        {
            options ??= new AccreditationOptions();

            Address? address = options.SiteAddress;
            if (address == null && options.IncludeSiteAddress)
            {
                address = options.IncludeFullAddress
                    ? CreateSiteAddress(scenarioId, "Test", includeFullAddress: true)
                    : CreateSiteAddress(scenarioId, "Test");
            }

            return new Accreditation
            {
                Id = $"stub-accred-{scenarioId}",
                AccreditationNumber = $"STUB-ACC-{scenarioId}",
                AccreditationYear = options.AccreditationYear,
                Material = material,
                SubmittedToRegulator = options.Regulator,
                GlassRecyclingProcess = options.GlassRecyclingProcess,
                SiteAddress = address,
            };
        }

        private static Address CreateSiteAddress(
            string scenarioId,
            string streetPrefix,
            bool includeFullAddress = false
        )
        {
            var address = new Address
            {
                Line1 = $"{scenarioId} {streetPrefix} Street",
                Postcode = $"T{scenarioId} 1AB",
            };

            if (includeFullAddress)
            {
                address.Line2 = "Unit 1";
                address.Town = $"{streetPrefix} Town";
                address.County = $"{streetPrefix} County";
                address.Country = "United Kingdom";
            }

            return address;
        }

        #endregion
    }
}
