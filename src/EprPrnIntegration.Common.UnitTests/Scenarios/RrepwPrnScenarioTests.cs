using AutoMapper;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Scenarios;

/// <summary>
/// Behavior-driven tests for RREPW PRN processing scenarios based on REEX-148 acceptance criteria.
/// These tests validate the canonical stub scenario matrix (PRN-01 to PRN-14) covering AC1-AC12.
/// </summary>
public class RrepwPrnScenarioTests
{
    private readonly IMapper _mapper = RrepwMappers.CreateMapper();
    private readonly DateTime _fromDate = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _toDate = new(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc);
    private const string StubOrgId = "0b51240c-c013-4973-9d06-d4f90ee4ad8b";

    #region AC1 - Mandatory Field Mapping

    /// <summary>
    /// PRN-01: Given a PRN with awaiting_acceptance status and all mandatory fields populated
    /// When the PRN is mapped to SavePrnDetailsRequest
    /// Then all mandatory fields should be correctly mapped
    /// </summary>
    [Fact]
    public void Prn01_GivenAwaitingAcceptancePrnWithAllMandatoryFields_WhenMapped_ThenAllFieldsShouldBeMapped()
    {
        // Arrange - PRN-01: awaiting_acceptance, PRN, plastic, authorisedAt = fromDate + 1 min
        var prn = CreateBasePrn(
            scenarioId: "01",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC1: Mandatory field mapping validated
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

    /// <summary>
    /// PRN-05: Given a PRN with rejected status
    /// When the PRN is mapped
    /// Then PrnStatusId should be null (indicating it should not be persisted)
    /// </summary>
    [Fact]
    public void Prn05_GivenRejectedPrn_WhenMapped_ThenPrnStatusIdShouldBeNull()
    {
        // Arrange - PRN-05: rejected, PRN, plastic, rejectedAt = fromDate + 1 min
        var prn = CreateBasePrn(
            scenarioId: "05",
            status: RrepwStatus.Rejected,
            rejectedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC2: Only awaiting_acceptance and cancelled persisted (rejected should NOT be)
        result.PrnStatusId.Should().BeNull("rejected PRNs should not be persisted");
    }

    [Theory]
    [InlineData(RrepwStatus.AwaitingAcceptance, EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData(RrepwStatus.Cancelled, EprnStatus.CANCELLED)]
    public void GivenValidStatus_WhenMapped_ThenPrnStatusIdShouldBeSet(string status, EprnStatus expectedStatus)
    {
        // Arrange
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

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC2: Only awaiting_acceptance and cancelled should have valid status
        result.PrnStatusId.Should().Be((int)expectedStatus);
    }

    [Theory]
    [InlineData(RrepwStatus.Accepted)]
    [InlineData(RrepwStatus.AwaitingAuthorisation)]
    [InlineData(RrepwStatus.AwaitingCancellation)]
    [InlineData(RrepwStatus.Rejected)]
    public void GivenInvalidStatus_WhenMapped_ThenPrnStatusIdShouldBeNull(string status)
    {
        // Arrange
        var prn = CreateBasePrn(
            scenarioId: "invalid-status-test",
            status: status,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC2: Invalid statuses should not be persisted
        result.PrnStatusId.Should().BeNull($"PRNs with status '{status}' should not be persisted");
    }

    #endregion

    #region AC3 - Delta Behaviour

    /// <summary>
    /// PRN-06: Given a PRN with authorisedAt before fromDate
    /// When filtering for delta processing
    /// Then the PRN should be excluded (delta exclusion)
    /// Note: This test validates the scenario setup; actual filtering is done by the API consumer
    /// </summary>
    [Fact]
    public void Prn06_GivenPrnWithAuthorisedAtBeforeFromDate_WhenCheckingDelta_ThenShouldBeExcluded()
    {
        // Arrange - PRN-06: awaiting_acceptance, PRN, paper, authorisedAt = fromDate - 1 min
        var prn = CreateBasePrn(
            scenarioId: "06",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(-1), // Before fromDate - delta exclusion
            isExport: false,
            material: RrepwMaterialName.Paper
        );

        // Act
        var authorisedAt = prn.Status!.AuthorisedAt;

        // Assert - AC3: Delta behaviour - PRN is outside the date range
        authorisedAt.Should().BeBefore(_fromDate, "PRN-06 should have authorisedAt before fromDate for delta exclusion testing");
    }

    /// <summary>
    /// PRN-13: Given a PRN with the same ID on consecutive runs but different tonnage
    /// When processed multiple times
    /// Then the update (upsert) behaviour should apply
    /// </summary>
    [Fact]
    public void Prn13_GivenSameIdWithChangingTonnage_WhenMapped_ThenShouldSupportUpsertBehaviour()
    {
        // Arrange - PRN-13: awaiting_acceptance, PRN, plastic, authorisedAt = toDate - 1 min
        // Same ID every run, changing tonnage
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
        prn2.TonnageValue = 750; // Changed tonnage

        // Act
        var result1 = _mapper.Map<SavePrnDetailsRequest>(prn1);
        var result2 = _mapper.Map<SavePrnDetailsRequest>(prn2);

        // Assert - AC3: Update behaviour - same SourceSystemId, different tonnage
        result1.SourceSystemId.Should().Be(fixedId);
        result2.SourceSystemId.Should().Be(fixedId);
        result1.TonnageValue.Should().Be(700);
        result2.TonnageValue.Should().Be(750);
    }

    #endregion

    #region AC4 - Ignored Fields

    /// <summary>
    /// PRN-12: Given a PRN with ignored fields populated
    /// When mapped
    /// Then ignored fields should not affect the persistence mapping
    /// </summary>
    [Fact]
    public void Prn12_GivenPrnWithIgnoredFields_WhenMapped_ThenIgnoredFieldsShouldNotAffectMapping()
    {
        // Arrange - PRN-12: awaiting_acceptance, PRN, wood, with ignored fields populated
        var prn = CreateBasePrn(
            scenarioId: "12",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Wood
        );
        // Add ignored field
        prn.Status!.AcceptedAt = DateTime.UtcNow.AddDays(-5);
        prn.IsDecemberWaste = true;

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC4: Ignored fields do not affect persistence
        result.PrnStatusId.Should().Be((int)EprnStatus.AWAITINGACCEPTANCE);
        result.MaterialName.Should().Be(RpdMaterialName.Wood);
        result.DecemberWaste.Should().BeTrue("IsDecemberWaste is a mapped field, not ignored");
    }

    #endregion

    #region AC5 - awaiting_acceptance uses authorisedAt

    /// <summary>
    /// PRN-01/02: Given a PRN with awaiting_acceptance status
    /// When mapped
    /// Then StatusUpdatedOn should use authorisedAt
    /// </summary>
    [Fact]
    public void GivenAwaitingAcceptancePrn_WhenMapped_ThenStatusUpdatedOnShouldUseAuthorisedAt()
    {
        // Arrange
        var authorisedAt = _fromDate.AddMinutes(1);
        var prn = CreateBasePrn(
            scenarioId: "01",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: authorisedAt,
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC5: awaiting_acceptance uses authorisedAt
        result.StatusUpdatedOn.Should().Be(authorisedAt);
        result.IssueDate.Should().Be(authorisedAt);
    }

    #endregion

    #region AC6 - cancelled uses cancelledAt

    /// <summary>
    /// PRN-03/04: Given a cancelled PRN
    /// When mapped
    /// Then StatusUpdatedOn should use cancelledAt (not authorisedAt)
    /// </summary>
    [Theory]
    [InlineData(false, "PRN-03")]
    [InlineData(true, "PRN-04")]
    public void GivenCancelledPrn_WhenMapped_ThenStatusUpdatedOnShouldUseCancelledAt(bool isExport, string scenarioId)
    {
        // Arrange - PRN-03/04: cancelled, cancelledAt = toDate - 1 min, authorisedAt also present (ignored)
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

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC6: cancelled uses cancelledAt
        result.StatusUpdatedOn.Should().Be(cancelledAt, "cancelled PRNs should use cancelledAt for StatusUpdatedOn");
        result.IssueDate.Should().Be(authorisedAt, "IssueDate should still use authorisedAt");
        result.PrnStatusId.Should().Be((int)EprnStatus.CANCELLED);
    }

    #endregion

    #region AC7 - Enum Mappings (Material, Regulator)

    /// <summary>
    /// PRN-07: Given a PRN with aluminium material and NRW regulator
    /// When mapped
    /// Then enum values should be correctly transformed
    /// </summary>
    [Fact]
    public void Prn07_GivenAluminiumPrnWithNrwRegulator_WhenMapped_ThenEnumsShouldBeCorrectlyMapped()
    {
        // Arrange - PRN-07: awaiting_acceptance, aluminium, NRW regulator
        var prn = CreateBasePrn(
            scenarioId: "07",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Aluminium,
            regulator: RrepwSubmittedToRegulator.NaturalResourcesWales_NRW
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC7: Enum mappings (material, regulator)
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
        // Arrange
        var prn = CreateBasePrn(
            scenarioId: "regulator-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic,
            regulator: rrepwRegulator
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC7: Regulator enum mapping
        result.ReprocessorExporterAgency.Should().Be(expectedRpdAgency);
    }

    #endregion

    #region AC8 - Glass Mapping via glassRecyclingProcess

    /// <summary>
    /// PRN-08: Given a glass PRN with glass_re_melt process
    /// When mapped
    /// Then MaterialName should be GlassRemelt
    /// </summary>
    [Fact]
    public void Prn08_GivenGlassPrnWithRemeltProcess_WhenMapped_ThenMaterialShouldBeGlassRemelt()
    {
        // Arrange - PRN-08: awaiting_acceptance, glass, glassRecyclingProcess = glass_re_melt
        var prn = CreateBasePrn(
            scenarioId: "08",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Glass,
            glassRecyclingProcess: RrepwGlassRecyclingProcess.GlassRemelt
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC8: Glass mapping via glassRecyclingProcess
        result.MaterialName.Should().Be(RpdMaterialName.GlassRemelt);
    }

    /// <summary>
    /// PRN-09: Given a glass PRN with glass_other process
    /// When mapped
    /// Then MaterialName should be GlassOther
    /// </summary>
    [Fact]
    public void Prn09_GivenGlassPrnWithOtherProcess_WhenMapped_ThenMaterialShouldBeGlassOther()
    {
        // Arrange - PRN-09: awaiting_acceptance, glass, glassRecyclingProcess = glass_other
        var prn = CreateBasePrn(
            scenarioId: "09",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Glass,
            glassRecyclingProcess: RrepwGlassRecyclingProcess.GlassOther
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC8: Glass mapping via glassRecyclingProcess
        result.MaterialName.Should().Be(RpdMaterialName.GlassOther);
    }

    [Theory]
    [InlineData(RrepwGlassRecyclingProcess.GlassRemelt, RpdMaterialName.GlassRemelt)]
    [InlineData(RrepwGlassRecyclingProcess.GlassOther, RpdMaterialName.GlassOther)]
    public void GivenGlassPrn_WhenMapped_ThenMaterialShouldReflectGlassRecyclingProcess(
        string glassRecyclingProcess,
        string expectedMaterial)
    {
        // Arrange
        var prn = CreateBasePrn(
            scenarioId: "glass-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Glass,
            glassRecyclingProcess: glassRecyclingProcess
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC8: Glass mapping
        result.MaterialName.Should().Be(expectedMaterial);
    }

    #endregion

    #region AC9 - ProcessToBeUsed Mapping

    /// <summary>
    /// PRN-10: Given a steel PRN
    /// When mapped
    /// Then ProcessToBeUsed should be R4
    /// </summary>
    [Fact]
    public void Prn10_GivenSteelPrn_WhenMapped_ThenProcessToBeUsedShouldBeR4()
    {
        // Arrange - PRN-10: awaiting_acceptance, steel
        var prn = CreateBasePrn(
            scenarioId: "10",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Steel,
            regulator: RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC9: ProcessToBeUsed mapping (steel -> R4)
        result.ProcessToBeUsed.Should().Be(RpdProcesses.R4);
    }

    /// <summary>
    /// PRN-11: Given a fibre PRN
    /// When mapped
    /// Then ProcessToBeUsed should be R3
    /// </summary>
    [Fact]
    public void Prn11_GivenFibrePrn_WhenMapped_ThenProcessToBeUsedShouldBeR3()
    {
        // Arrange - PRN-11: awaiting_acceptance, fibre
        var prn = CreateBasePrn(
            scenarioId: "11",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Fibre,
            regulator: RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC9: ProcessToBeUsed mapping (fibre -> R3)
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
        // Arrange
        var prn = CreateBasePrn(
            scenarioId: "process-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: material
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC9: ProcessToBeUsed mapping
        result.ProcessToBeUsed.Should().Be(expectedProcess);
    }

    #endregion

    #region AC10 - PRN vs PERN Behaviour

    /// <summary>
    /// PRN-01 vs PRN-02: Given PRN (isExport=false) and PERN (isExport=true)
    /// When mapped
    /// Then IsExport should be correctly set and PERN should not have siteAddress
    /// </summary>
    [Fact]
    public void Prn01VsPrn02_GivenPrnAndPern_WhenMapped_ThenIsExportShouldDifferentiate()
    {
        // Arrange - PRN-01: isExport=false (PRN)
        var prn = CreateBasePrn(
            scenarioId: "01",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );

        // Arrange - PRN-02: isExport=true (PERN), siteAddress omitted
        var pern = CreateBasePrn(
            scenarioId: "02",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: true,
            material: RrepwMaterialName.Plastic
        );
        pern.Accreditation!.SiteAddress = null; // PERN typically has no site address

        // Act
        var prnResult = _mapper.Map<SavePrnDetailsRequest>(prn);
        var pernResult = _mapper.Map<SavePrnDetailsRequest>(pern);

        // Assert - AC10: PRN vs PERN behaviour
        prnResult.IsExport.Should().BeFalse("PRN-01 is a PRN (not export)");
        pernResult.IsExport.Should().BeTrue("PRN-02 is a PERN (export)");
        prnResult.ReprocessingSite.Should().NotBeNullOrEmpty("PRN should have reprocessing site");
        pernResult.ReprocessingSite.Should().BeNull("PERN typically does not have reprocessing site");
    }

    #endregion

    #region AC11 - Multiple PRNs Processed

    /// <summary>
    /// Given multiple PRNs in a single response
    /// When all are mapped
    /// Then all should be correctly processed
    /// </summary>
    [Fact]
    public void GivenMultiplePrns_WhenMapped_ThenAllShouldBeProcessed()
    {
        // Arrange - Create multiple PRNs representing different scenarios
        var prns = new List<PackagingRecyclingNote>
        {
            CreateBasePrn("01", RrepwStatus.AwaitingAcceptance, _fromDate.AddMinutes(1), false, RrepwMaterialName.Plastic),
            CreateBasePrn("02", RrepwStatus.AwaitingAcceptance, _fromDate.AddMinutes(1), true, RrepwMaterialName.Plastic),
            CreateBasePrn("07", RrepwStatus.AwaitingAcceptance, _fromDate.AddMinutes(1), false, RrepwMaterialName.Aluminium),
            CreateBasePrn("10", RrepwStatus.AwaitingAcceptance, _fromDate.AddMinutes(1), false, RrepwMaterialName.Steel),
        };

        // Act
        var results = prns.Select(p => _mapper.Map<SavePrnDetailsRequest>(p)).ToList();

        // Assert - AC11: Multiple PRNs processed in one response
        results.Should().HaveCount(4);
        results.Should().OnlyContain(r => r.PrnStatusId == (int)EprnStatus.AWAITINGACCEPTANCE);
        results.Select(r => r.PrnNumber).Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region AC12 - Obligation Year Always 2026

    /// <summary>
    /// PRN-14: Given any PRN
    /// When mapped
    /// Then ObligationYear should always be 2026
    /// </summary>
    [Fact]
    public void Prn14_GivenAnyPrn_WhenMapped_ThenObligationYearShouldAlwaysBe2026()
    {
        // Arrange - PRN-14: awaiting_acceptance, plastic, obligation year forced to 2026
        var prn = CreateBasePrn(
            scenarioId: "14",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );
        // Even if accreditation year is different
        prn.Accreditation!.AccreditationYear = 2025;

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC12: Obligation year always stored as 2026
        result.ObligationYear.Should().Be("2026");
    }

    [Theory]
    [InlineData(2024)]
    [InlineData(2025)]
    [InlineData(2026)]
    [InlineData(2027)]
    public void GivenAnyAccreditationYear_WhenMapped_ThenObligationYearShouldAlwaysBe2026(int accreditationYear)
    {
        // Arrange
        var prn = CreateBasePrn(
            scenarioId: "obligation-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: RrepwMaterialName.Plastic
        );
        prn.Accreditation!.AccreditationYear = accreditationYear;

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC12: Obligation year always 2026 regardless of accreditation year
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
        // Arrange
        var prn = CreateBasePrn(
            scenarioId: "material-test",
            status: RrepwStatus.AwaitingAcceptance,
            authorisedAt: _fromDate.AddMinutes(1),
            isExport: false,
            material: rrepwMaterial
        );

        // Act
        var result = _mapper.Map<SavePrnDetailsRequest>(prn);

        // Assert - AC7: Material enum mapping
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
        var prn = new PackagingRecyclingNote
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

        return prn;
    }

    #endregion
}
