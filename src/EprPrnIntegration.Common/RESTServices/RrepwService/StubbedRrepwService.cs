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
        // Fixed ID for PRN-13 to test update/upsert behaviour
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
                // PRN-01: awaiting_acceptance, PRN, plastic, authorisedAt = fromDate + 1 min
                // AC1 (mandatory field mapping), AC5 (awaiting_acceptance uses authorisedAt)
                CreatePrn(
                    scenarioId: "01",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("01", dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 100,
                    accreditation: CreateAccreditation("01", RrepwMaterialName.Plastic, includeFullAddress: true)
                ),

                // PRN-02: awaiting_acceptance, PERN (isExport=true), plastic, authorisedAt = fromDate + 1 min
                // AC1 (mandatory field mapping), AC5, AC10 (PRN vs PERN behaviour)
                CreatePrn(
                    scenarioId: "02",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("02", dateFrom.AddMinutes(1)),
                    isExport: true,
                    stubOrgId: stubOrgId,
                    tonnageValue: 150,
                    accreditation: CreateAccreditation("02", RrepwMaterialName.Plastic, includeSiteAddress: false) // siteAddress omitted for PERN
                ),

                // PRN-03: cancelled, PRN, plastic, cancelledAt = toDate - 1 min
                // AC6 (cancelled uses cancelledAt)
                CreatePrn(
                    scenarioId: "03",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateCancelledStatus("03", dateTo.AddMinutes(-1), dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 200,
                    accreditation: CreateAccreditation("03", RrepwMaterialName.Plastic),
                    issuerNotes: "Stubbed cancelled PRN-03 for AC6 testing"
                ),

                // PRN-04: cancelled, PERN (isExport=true), plastic, cancelledAt = toDate - 1 min
                // AC6 (cancelled uses cancelledAt)
                CreatePrn(
                    scenarioId: "04",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateCancelledStatus("04", dateTo.AddMinutes(-1), dateFrom.AddMinutes(1)),
                    isExport: true,
                    stubOrgId: stubOrgId,
                    tonnageValue: 250,
                    accreditation: CreateAccreditation("04", RrepwMaterialName.Plastic),
                    issuerNotes: "Stubbed cancelled PRN-04 for AC6 testing"
                ),

                // PRN-05: rejected, PRN, plastic, rejectedAt = fromDate + 1 min
                // AC2 (only awaiting_acceptance and cancelled persisted - this should NOT be persisted)
                CreatePrn(
                    scenarioId: "05",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateRejectedStatus(dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 300,
                    accreditation: CreateAccreditation("05", RrepwMaterialName.Plastic, includeSiteAddress: false),
                    includeIssuedByTradingName: false,
                    includeIssuedToTradingName: false,
                    issuerNotes: "Stubbed rejected PRN-05 - should NOT be persisted (AC2)"
                ),

                // PRN-06: awaiting_acceptance, PRN, paper, authorisedAt = fromDate - 1 min
                // AC3 (delta exclusion - outside date range, should NOT be persisted)
                CreatePrn(
                    scenarioId: "06",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("06", dateFrom.AddMinutes(-1)), // Before fromDate - delta exclusion
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 350,
                    accreditation: CreateAccreditation("06", RrepwMaterialName.Paper, includeFullAddress: true)
                ),

                // PRN-07: awaiting_acceptance, PRN, aluminium, authorisedAt = fromDate + 1 min
                // AC7 (enum mapping for material and regulator)
                CreatePrn(
                    scenarioId: "07",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("07", dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 400,
                    accreditation: CreateAccreditation(
                        "07",
                        RrepwMaterialName.Aluminium,
                        regulator: RrepwSubmittedToRegulator.NaturalResourcesWales_NRW,
                        includeFullAddress: true
                    )
                ),

                // PRN-08: awaiting_acceptance, PRN, glass (glass_re_melt), authorisedAt = fromDate + 1 min
                // AC8 (glass mapping via glassRecyclingProcess)
                CreatePrn(
                    scenarioId: "08",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("08", dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 450,
                    accreditation: CreateAccreditation(
                        "08",
                        RrepwMaterialName.Glass,
                        glassRecyclingProcess: RrepwGlassRecyclingProcess.GlassRemelt,
                        siteAddress: CreateSiteAddress("08", "Glass")
                    ),
                    issuerNotes: $"Stubbed glass PRN-08 for AC8 testing ({RrepwGlassRecyclingProcess.GlassRemelt})"
                ),

                // PRN-09: awaiting_acceptance, PRN, glass (glass_other), authorisedAt = fromDate + 1 min
                // AC8 (glass mapping via glassRecyclingProcess)
                CreatePrn(
                    scenarioId: "09",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("09", dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 500,
                    accreditation: CreateAccreditation(
                        "09",
                        RrepwMaterialName.Glass,
                        glassRecyclingProcess: RrepwGlassRecyclingProcess.GlassOther,
                        siteAddress: CreateSiteAddress("09", "Glass")
                    ),
                    issuerNotes: $"Stubbed glass PRN-09 for AC8 testing ({RrepwGlassRecyclingProcess.GlassOther})"
                ),

                // PRN-10: awaiting_acceptance, PRN, steel, authorisedAt = fromDate + 1 min
                // AC9 (ProcessToBeUsed mapping - steel maps to R4)
                CreatePrn(
                    scenarioId: "10",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("10", dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 550,
                    accreditation: CreateAccreditation(
                        "10",
                        RrepwMaterialName.Steel,
                        regulator: RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA,
                        includeFullAddress: true
                    )
                ),

                // PRN-11: awaiting_acceptance, PRN, fibre, authorisedAt = fromDate + 1 min
                // AC9 (ProcessToBeUsed mapping - fibre maps to R3)
                CreatePrn(
                    scenarioId: "11",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("11", dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 600,
                    accreditation: CreateAccreditation(
                        "11",
                        RrepwMaterialName.Fibre,
                        regulator: RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA,
                        includeFullAddress: true
                    )
                ),

                // PRN-12: awaiting_acceptance, PRN, wood, authorisedAt = fromDate + 1 min
                // AC4 (ignored fields do not affect persistence)
                CreatePrn(
                    scenarioId: "12",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatusWithIgnoredFields("12", dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 650,
                    accreditation: CreateAccreditation(
                        "12",
                        RrepwMaterialName.Wood,
                        siteAddress: CreateSiteAddress("12", "Wood", includeFullAddress: true)
                    ),
                    isDecemberWaste: true, // This is a mapped field, not ignored
                    issuerNotes: "Stubbed PRN-12 with ignored fields for AC4 testing"
                ),

                // PRN-13: awaiting_acceptance, PRN, plastic, authorisedAt = toDate - 1 min
                // AC3 (update behaviour - same ID every run, changing tonnage)
                CreateUpdateTestPrn(
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    authorisedAt: dateTo.AddMinutes(-1),
                    stubOrgId: stubOrgId,
                    // Tonnage varies based on current minute to simulate changes between runs
                    tonnageValue: 700 + now.Minute
                ),

                // PRN-14: awaiting_acceptance, PRN, plastic, authorisedAt = fromDate + 1 min
                // AC12 (obligation year always stored as 2026)
                CreatePrn(
                    scenarioId: "14",
                    hourlyPrnSuffix: hourlyPrnSuffix,
                    status: CreateAwaitingAcceptanceStatus("14", dateFrom.AddMinutes(1)),
                    isExport: false,
                    stubOrgId: stubOrgId,
                    tonnageValue: 800,
                    accreditation: CreateAccreditation("14", RrepwMaterialName.Plastic, accreditationYear: 2026, includeFullAddress: true)
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

        #region PRN Factory Methods

        private static PackagingRecyclingNote CreatePrn(
            string scenarioId,
            string hourlyPrnSuffix,
            Status status,
            bool isExport,
            string stubOrgId,
            int tonnageValue,
            Accreditation accreditation,
            bool includeIssuedByTradingName = true,
            bool includeIssuedToTradingName = true,
            bool isDecemberWaste = false,
            string? issuerNotes = null
        )
        {
            return new PackagingRecyclingNote
            {
                Id = $"stub-prn-{scenarioId}-{Guid.NewGuid()}",
                PrnNumber = $"STUB-PRN{scenarioId}-{hourlyPrnSuffix}",
                Status = status,
                IssuedByOrganisation = CreateIssuedByOrganisation(scenarioId, includeIssuedByTradingName),
                IssuedToOrganisation = CreateIssuedToOrganisation(scenarioId, stubOrgId, includeIssuedToTradingName),
                Accreditation = accreditation,
                IsDecemberWaste = isDecemberWaste,
                IsExport = isExport,
                TonnageValue = tonnageValue,
                IssuerNotes = issuerNotes ?? $"Stubbed PRN-{scenarioId} for AC testing",
            };
        }

        private static PackagingRecyclingNote CreateUpdateTestPrn(
            string hourlyPrnSuffix,
            DateTime authorisedAt,
            string stubOrgId,
            int tonnageValue
        )
        {
            // PRN-13: Fixed ID for update/upsert testing
            // Same ID and prnNumber every run, but tonnage changes
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
                TonnageValue = tonnageValue, // Changes each run to test update behaviour
                IssuerNotes = $"Stubbed PRN-13 for AC3 update testing - tonnage: {tonnageValue}",
            };
        }

        #endregion

        #region Status Factory Methods

        private static Status CreateAwaitingAcceptanceStatus(string scenarioId, DateTime authorisedAt)
        {
            return new Status
            {
                CurrentStatus = RrepwStatus.AwaitingAcceptance,
                AuthorisedAt = authorisedAt,
                AuthorisedBy = CreateAuthorisedBy(scenarioId),
            };
        }

        private static Status CreateAwaitingAcceptanceStatusWithIgnoredFields(string scenarioId, DateTime authorisedAt)
        {
            return new Status
            {
                CurrentStatus = RrepwStatus.AwaitingAcceptance,
                AuthorisedAt = authorisedAt,
                AuthorisedBy = CreateAuthorisedBy(scenarioId),
                // Ignored fields - should not affect persistence
                AcceptedAt = DateTime.UtcNow.AddDays(-5),
            };
        }

        private static Status CreateCancelledStatus(string scenarioId, DateTime cancelledAt, DateTime authorisedAt)
        {
            return new Status
            {
                CurrentStatus = RrepwStatus.Cancelled,
                CancelledAt = cancelledAt,
                AuthorisedAt = authorisedAt, // Also present but should be ignored for cancelled
                AuthorisedBy = CreateAuthorisedBy(scenarioId),
            };
        }

        private static Status CreateRejectedStatus(DateTime rejectedAt)
        {
            return new Status
            {
                CurrentStatus = RrepwStatus.Rejected,
                RejectedAt = rejectedAt,
            };
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

        private static Organisation CreateIssuedByOrganisation(string scenarioId, bool includeTradingName = true)
        {
            return new Organisation
            {
                Id = $"issuer-org-{scenarioId}",
                Name = $"Stubbed Issuer Organisation {scenarioId}",
                TradingName = includeTradingName ? $"Stubbed Issuer Trading {scenarioId}" : null,
            };
        }

        private static Organisation CreateIssuedToOrganisation(string scenarioId, string stubOrgId, bool includeTradingName = true)
        {
            return new Organisation
            {
                Id = stubOrgId,
                Name = $"Stubbed Recipient Organisation {scenarioId}",
                TradingName = includeTradingName ? $"Stubbed Recipient Trading {scenarioId}" : null,
            };
        }

        #endregion

        #region Accreditation Factory Methods

        private static Accreditation CreateAccreditation(
            string scenarioId,
            string material,
            string regulator = RrepwSubmittedToRegulator.EnvironmentAgency_EA,
            int accreditationYear = 2026,
            Address? siteAddress = null,
            string? glassRecyclingProcess = null,
            bool includeSiteAddress = true,
            bool includeFullAddress = false
        )
        {
            Address? address = siteAddress;
            if (address == null && includeSiteAddress)
            {
                address = includeFullAddress
                    ? CreateSiteAddress(scenarioId, "Test", includeFullAddress: true)
                    : CreateSiteAddress(scenarioId, "Test");
            }

            return new Accreditation
            {
                Id = $"stub-accred-{scenarioId}",
                AccreditationNumber = $"STUB-ACC-{scenarioId}",
                AccreditationYear = accreditationYear,
                Material = material,
                SubmittedToRegulator = regulator,
                GlassRecyclingProcess = glassRecyclingProcess,
                SiteAddress = address,
            };
        }

        private static Address CreateSiteAddress(string scenarioId, string streetPrefix, bool includeFullAddress = false)
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
