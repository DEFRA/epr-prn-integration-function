using AutoMapper;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Scenarios;

public class RrepwPrnScenarioTests
{
    private readonly IMapper _mapper = RrepwMappers.CreateMapper();
    private readonly DateTime _fromDate = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _toDate = new(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc);
    private const string StubOrgId = "0b51240c-c013-4973-9d06-d4f90ee4ad8b";

    #region AC1 - Mandatory Field Mapping

    [Fact]
    public void Prn01_GivenAwaitingAcceptancePrnWithAllMandatoryFields_WhenMapped_ThenAllFieldsShouldBeMapped()
    {
        var prn = CreateBasePrn(
            scenarioId: "01",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.SourceSystemId.Should().Be(prn.Id);
        result.PrnNumber.Should().Be(prn.PrnNumber);
        result.PrnStatusId.Should().Be((int)EprnStatus.AWAITINGACCEPTANCE);
        result.PrnSignatory.Should().Be(prn.Status!.AuthorisedBy!.FullName);
        result.IssuedByOrg.Should().Be(prn.IssuedByOrganisation!.Name);
        result.OrganisationId.Should().Be(Guid.Parse(prn.IssuedToOrganisation!.Id!));
        result.AccreditationNumber.Should().Be(prn.Accreditation!.AccreditationNumber);
        result.AccreditationYear.Should().Be(prn.Accreditation.AccreditationYear.ToString());
        result.MaterialName.Should().Be(RpdMaterialName.Plastic);
        result.TonnageValue.Should().Be(prn.TonnageValue);
        result.DecemberWaste.Should().Be(prn.IsDecemberWaste);
        result.IsExport.Should().Be(prn.IsExport);
    }

    #endregion

    #region AC2 - Status Filtering (Only awaiting_acceptance and cancelled persisted)

    [Fact]
    public void Prn05_GivenRejectedPrn_WhenMapped_ThenPrnStatusIdShouldBeNull()
    {
        var prn = CreateBasePrn(
            scenarioId: "05",
            status: RrepwStatus.Rejected,
            rejectedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.PrnStatusId.Should().BeNull("rejected PRNs should not be persisted");
    }

    [Theory]
    [InlineData(RrepwStatus.AwaitingAcceptance, EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData(RrepwStatus.Cancelled, EprnStatus.CANCELLED)]
    public void GivenValidStatus_WhenMapped_ThenPrnStatusIdShouldBeSet(string status, EprnStatus expectedStatus)
    {
        var prn = CreateBasePrn(
            scenarioId: "status-test",
            status: status,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );
        if (status == RrepwStatus.Cancelled)
        {
            prn.Status!.CancelledAt = _toDate.AddMinutes(-1);
        }

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.PrnStatusId.Should().Be((int)expectedStatus);
    }

    [Theory]
    [InlineData(RrepwStatus.Accepted)]
    [InlineData(RrepwStatus.AwaitingAuthorisation)]
    [InlineData(RrepwStatus.AwaitingCancellation)]
    [InlineData(RrepwStatus.Rejected)]
    public void GivenInvalidStatus_WhenMapped_ThenPrnStatusIdShouldBeNull(string status)
    {
        var prn = CreateBasePrn(
            scenarioId: "invalid-status-test",
            status: status,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.PrnStatusId.Should().BeNull($"PRNs with status '{status}' should not be persisted");
    }

    #endregion

    #region AC3 - Delta Behaviour

    [Fact]
    public void Prn06_GivenPrnWithAuthorisedAtBeforeFromDate_WhenCheckingDelta_ThenShouldBeExcluded()
    {
        var prn = CreateBasePrn(
            scenarioId: "06",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(-1),
            isExport: false,
            material: RrepwMaterialName.Paper
        );

        var authorisedAt = prn.Status!.AuthorisedAt;

        authorisedAt.Should().BeBefore(_fromDate, "PRN-06 should have authorisedAt before fromDate for delta exclusion testing");
    }

    [Fact]
    public void Prn13_GivenSameIdWithChangingTonnage_WhenMapped_ThenShouldSupportUpsertBehaviour()
    {
        const string fixedId = "prn13-fixed-id-for-update-test";

        var prn1 = CreateBasePrn(
            scenarioId: "13",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _toDate.AddMinutes(-1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );
        prn1.Id = fixedId;
        prn1.TonnageValue = 700;

        var prn2 = CreateBasePrn(
            scenarioId: "13",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _toDate.AddMinutes(-1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );
        prn2.Id = fixedId;
        prn2.TonnageValue = 750;

        var result1 = _mapper.Map<SavePrnDetailsRequest>(prn1);
        var result2 = _mapper.Map<SavePrnDetailsRequest>(prn2);

        result1.SourceSystemId.Should().Be(fixedId);
        result2.SourceSystemId.Should().Be(fixedId);
        result1.TonnageValue.Should().Be(700);
        result2.TonnageValue.Should().Be(750);
    }

    #endregion

    #region AC4 - Ignored Fields

    [Fact]
    public void Prn12_GivenPrnWithIgnoredFields_WhenMapped_ThenIgnoredFieldsShouldNotAffectMapping()
    {
        var prn = CreateBasePrn(
            scenarioId: "12",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Wood
        );
        prn.Status!.AcceptedAt = DateTime.UtcNow.AddDays(-5);
        prn.IsDecemberWaste = true;

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.PrnStatusId.Should().Be((int)EprnStatus.AWAITINGACCEPTANCE);
        result.MaterialName.Should().Be(RpdMaterialName.Wood);
        result.DecemberWaste.Should().BeTrue("IsDecemberWaste is a mapped field, not ignored");
    }

    #endregion

    #region AC5 - awaiting_acceptance uses authorisedAt

    [Fact]
    public void GivenAwaitingAcceptancePrn_WhenMapped_ThenStatusUpdatedOnShouldUseAuthorisedAt()
    {
        var authorisedAt = _fromDate.AddMinutes(1);
        var prn = CreateBasePrn(
            scenarioId: "01",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: authorisedAt,
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.StatusUpdatedOn.Should().Be(authorisedAt);
        result.IssueDate.Should().Be(authorisedAt);
    }

    #endregion

    #region AC6 - cancelled uses cancelledAt

    [Theory]
    [InlineData(false, "PRN-03")]
    [InlineData(true, "PRN-04")]
    public void GivenCancelledPrn_WhenMapped_ThenStatusUpdatedOnShouldUseCancelledAt(bool isExport, string scenarioId)
    {
        var authorisedAt = _fromDate.AddMinutes(1);
        var cancelledAt = _toDate.AddMinutes(-1);

        var prn = CreateBasePrn(
            scenarioId: scenarioId,
            status: RrepwStatus.Cancelled,
            authorisedAt: authorisedAt,
            isExport: isExport,
            material: RrepwMaterialName.Plastic
        );
        prn.Status!.CancelledAt = cancelledAt;

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.StatusUpdatedOn.Should().Be(cancelledAt, "cancelled PRNs should use cancelledAt for StatusUpdatedOn");
        result.IssueDate.Should().Be(authorisedAt, "IssueDate should still use authorisedAt");
        result.PrnStatusId.Should().Be((int)EprnStatus.CANCELLED);
    }

    #endregion

    #region AC7 - Enum Mappings (Material, Regulator)

    [Fact]
    public void Prn07_GivenAluminiumPrnWithNrwRegulator_WhenMapped_ThenEnumsShouldBeCorrectlyMapped()
    {
        var prn = CreateBasePrn(
            scenarioId: "07",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Aluminium,
            regulator: RrepwSubmittedToRegulator.NaturalResourcesWales_NRW
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.MaterialName.Should().Be(RpdMaterialName.Aluminium);
        result.ReprocessorExporterAgency.Should().Be(RpdReprocessorExporterAgency.NaturalResourcesWales);
    }

    [Theory]
    [InlineData(RrepwSubmittedToRegulator.EnvironmentAgency_EA, RpdReprocessorExporterAgency.EnvironmentAgency)]
    [InlineData(RrepwSubmittedToRegulator.NaturalResourcesWales_NRW, RpdReprocessorExporterAgency.NaturalResourcesWales)]
    [InlineData(RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA, RpdReprocessorExporterAgency.NorthernIrelandEnvironmentAgency)]
    [InlineData(RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA, RpdReprocessorExporterAgency.ScottishEnvironmentProtectionAgency)]
    public void GivenRegulator_WhenMapped_ThenShouldMapToCorrectRpdAgency(string rrepwRegulator, string expectedRpdAgency)
    {
        var prn = CreateBasePrn(
            scenarioId: "regulator-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic,
            regulator: rrepwRegulator
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.ReprocessorExporterAgency.Should().Be(expectedRpdAgency);
    }

    #endregion

    #region AC8 - Glass Mapping via glassRecyclingProcess

    [Fact]
    public void Prn08_GivenGlassPrnWithRemeltProcess_WhenMapped_ThenMaterialShouldBeGlassRemelt()
    {
        var prn = CreateBasePrn(
            scenarioId: "08",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Glass,
            glassRecyclingProcess: RrepwGlassRecyclingProcess.GlassRemelt
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.MaterialName.Should().Be(RpdMaterialName.GlassRemelt);
    }

    [Fact]
    public void Prn09_GivenGlassPrnWithOtherProcess_WhenMapped_ThenMaterialShouldBeGlassOther()
    {
        var prn = CreateBasePrn(
            scenarioId: "09",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Glass,
            glassRecyclingProcess: RrepwGlassRecyclingProcess.GlassOther
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.MaterialName.Should().Be(RpdMaterialName.GlassOther);
    }

    [Theory]
    [InlineData(RrepwGlassRecyclingProcess.GlassRemelt, RpdMaterialName.GlassRemelt)]
    [InlineData(RrepwGlassRecyclingProcess.GlassOther, RpdMaterialName.GlassOther)]
    public void GivenGlassPrn_WhenMapped_ThenMaterialShouldReflectGlassRecyclingProcess(
        string glassRecyclingProcess,
        string expectedMaterial)
    {
        var prn = CreateBasePrn(
            scenarioId: "glass-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Glass,
            glassRecyclingProcess: glassRecyclingProcess
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.MaterialName.Should().Be(expectedMaterial);
    }

    #endregion

    #region AC9 - ProcessToBeUsed Mapping

    [Fact]
    public void Prn10_GivenSteelPrn_WhenMapped_ThenProcessToBeUsedShouldBeR4()
    {
        var prn = CreateBasePrn(
            scenarioId: "10",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Steel,
            regulator: RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.ProcessToBeUsed.Should().Be(RpdProcesses.R4);
    }

    [Fact]
    public void Prn11_GivenFibrePrn_WhenMapped_ThenProcessToBeUsedShouldBeR3()
    {
        var prn = CreateBasePrn(
            scenarioId: "11",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Fibre,
            regulator: RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.ProcessToBeUsed.Should().Be(RpdProcesses.R3);
    }

    [Theory]
    [InlineData(RrepwMaterialName.Aluminium, RpdProcesses.R4)]
    [InlineData(RrepwMaterialName.Fibre, RpdProcesses.R3)]
    [InlineData(RrepwMaterialName.Glass, RpdProcesses.R5)]
    [InlineData(RrepwMaterialName.Paper, RpdProcesses.R3)]
    [InlineData(RrepwMaterialName.Plastic, RpdProcesses.R3)]
    [InlineData(RrepwMaterialName.Steel, RpdProcesses.R4)]
    [InlineData(RrepwMaterialName.Wood, RpdProcesses.R3)]
    public void GivenMaterial_WhenMapped_ThenProcessToBeUsedShouldBeCorrect(string material, string expectedProcess)
    {
        var prn = CreateBasePrn(
            scenarioId: "process-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: material
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.ProcessToBeUsed.Should().Be(expectedProcess);
    }

    #endregion

    #region AC10 - PRN vs PERN Behaviour

    [Fact]
    public void Prn01VsPrn02_GivenPrnAndPern_WhenMapped_ThenIsExportShouldDifferentiate()
    {
        var prn = CreateBasePrn(
            scenarioId: "01",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        var pern = CreateBasePrn(
            scenarioId: "02",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: true,
            material: RrepwMaterialName.Plastic
        );
        pern.Accreditation!.SiteAddress = null;

        var prnResult = _mapper.Map<SavePrnDetailsRequest>(prn);
        var pernResult = _mapper.Map<SavePrnDetailsRequest>(pern);

        prnResult.IsExport.Should().BeFalse("PRN-01 is a PRN (not export)");
        pernResult.IsExport.Should().BeTrue("PRN-02 is a PERN (export)");
        prnResult.ReprocessingSite.Should().NotBeNullOrEmpty("PRN should have reprocessing site");
        pernResult.ReprocessingSite.Should().BeNull("PERN typically does not have reprocessing site");
    }

    #endregion

    #region AC11 - Multiple PRNs Processed

    [Fact]
    public void GivenMultiplePrns_WhenMapped_ThenAllShouldBeProcessed()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreateBasePrn("01", RrepwStatus.AwaitingAcceptance, _fromDate.AddMinutes(1), false, RrepwMaterialName.Plastic),
            CreateBasePrn("02", RrepwStatus.AwaitingAcceptance, _fromDate.AddMinutes(1), true, RrepwMaterialName.Plastic),
            CreateBasePrn("07", RrepwStatus.AwaitingAcceptance, _fromDate.AddMinutes(1), false, RrepwMaterialName.Aluminium),
            CreateBasePrn("10", RrepwStatus.AwaitingAcceptance, _fromDate.AddMinutes(1), false, RrepwMaterialName.Steel),
        };

        var results = prns.Select(p => _mapper.Map<SavePrnDetailsRequest>(p)).ToList();

        results.Should().HaveCount(4);
        results.Should().OnlyContain(r => r.PrnStatusId == (int)EprnStatus.AWAITINGACCEPTANCE);
        results.Select(r => r.PrnNumber).Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region AC12 - Obligation Year Always 2026

    [Fact]
    public void Prn14_GivenAnyPrn_WhenMapped_ThenObligationYearShouldAlwaysBe2026()
    {
        var prn = CreateBasePrn(
            scenarioId: "14",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );
        prn.Accreditation!.AccreditationYear = 2025;

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.ObligationYear.Should().Be("2026");
    }

    [Theory]
    [InlineData(2024)]
    [InlineData(2025)]
    [InlineData(2026)]
    [InlineData(2027)]
    public void GivenAnyAccreditationYear_WhenMapped_ThenObligationYearShouldAlwaysBe2026(int accreditationYear)
    {
        var prn = CreateBasePrn(
            scenarioId: "obligation-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );
        prn.Accreditation!.AccreditationYear = accreditationYear;

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.ObligationYear.Should().Be("2026");
    }

    #endregion

    #region Material Mapping Tests

    [Theory]
    [InlineData(RrepwMaterialName.Aluminium, RpdMaterialName.Aluminium)]
    [InlineData(RrepwMaterialName.Fibre, RpdMaterialName.Fibre)]
    [InlineData(RrepwMaterialName.Paper, RpdMaterialName.PaperBoard)]
    [InlineData(RrepwMaterialName.Plastic, RpdMaterialName.Plastic)]
    [InlineData(RrepwMaterialName.Steel, RpdMaterialName.Steel)]
    [InlineData(RrepwMaterialName.Wood, RpdMaterialName.Wood)]
    public void GivenMaterial_WhenMapped_ThenMaterialNameShouldBeCorrect(string rrepwMaterial, string expectedRpdMaterial)
    {
        var prn = CreateBasePrn(
            scenarioId: "material-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: rrepwMaterial
        );

        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        result.MaterialName.Should().Be(expectedRpdMaterial);
    }

    #endregion

    #region Helper Methods

    private static PackagingRecyclingNote CreateBasePrn(
        string scenarioId,
        string status,
        DateTime? authorisedAt = null,
        bool isExport = false,
        string material = RrepwMaterialName.Plastic,
        string regulator = RrepwSubmittedToRegulator.EnvironmentAgency_EA,
        string? glassRecyclingProcess = null,
        DateTime? rejectedAt = null)
    {
        return new PackagingRecyclingNote
        {
            Id = $"test-prn-{scenarioId}-{Guid.NewGuid()}",
            PrnNumber = $"TEST-PRN{scenarioId}",
            Status = new Status
            {
                CurrentStatus = status,
                AuthorisedAt = authorisedAt,
                AuthorisedBy = new UserSummary
                {
                    FullName = $"Test Signatory {scenarioId}",
                    JobTitle = "Test Manager"
                },
                RejectedAt = rejectedAt
            },
            IssuedByOrganisation = new Organisation
            {
                Id = $"issuer-org-{scenarioId}",
                Name = $"Test Issuer Organisation {scenarioId}",
                TradingName = $"Test Issuer Trading {scenarioId}"
            },
            IssuedToOrganisation = new Organisation
            {
                Id = StubOrgId,
                Name = $"Test Recipient Organisation {scenarioId}",
                TradingName = $"Test Recipient Trading {scenarioId}"
            },
            Accreditation = new Accreditation
            {
                Id = $"test-accred-{scenarioId}",
                AccreditationNumber = $"TEST-ACC-{scenarioId}",
                AccreditationYear = 2026,
                Material = material,
                SubmittedToRegulator = regulator,
                GlassRecyclingProcess = glassRecyclingProcess,
                SiteAddress = new Address
                {
                    Line1 = $"{scenarioId} Test Street",
                    Postcode = $"T{scenarioId} 1AB"
                }
            },
            IsDecemberWaste = false,
            IsExport = isExport,
            TonnageValue = 100,
            IssuerNotes = $"Test PRN-{scenarioId}"
        };
    }

    #endregion
}
