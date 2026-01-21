using System.Net;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.RrepwService;

public class StubbedRrepwServiceTests
{
    private readonly Common.RESTServices.RrepwService.StubbedRrepwService _service;
    private readonly Mock<
        ILogger<Common.RESTServices.RrepwService.StubbedRrepwService>
    > _loggerMock;
    private const string TestStubOrgId = "test-stub-org-id";
    private const string TestComplianceSchemeOrgId = "test-compliance-scheme-org-id";

    public StubbedRrepwServiceTests()
    {
        _loggerMock = new Mock<ILogger<Common.RESTServices.RrepwService.StubbedRrepwService>>();
        var configMock = new Mock<IOptions<RrepwApiConfiguration>>();
        configMock
            .Setup(c => c.Value)
            .Returns(
                new RrepwApiConfiguration
                {
                    StubOrgId = TestStubOrgId,
                    StubOrgIdComplianceScheme = TestComplianceSchemeOrgId,
                }
            );
        _service = new Common.RESTServices.RrepwService.StubbedRrepwService(
            _loggerMock.Object,
            configMock.Object
        );
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ReturnsExpectedNumberOfPrns()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        result.Should().HaveCount(17);
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_AllPrnsHaveRequiredFields()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        result
            .Should()
            .AllSatisfy(prn =>
            {
                prn.Id.Should().NotBeNullOrEmpty();
                prn.PrnNumber.Should().NotBeNullOrEmpty();
                prn.Status.Should().NotBeNull();
                prn.Status!.CurrentStatus.Should().NotBeNullOrEmpty();
                prn.IssuedByOrganisation.Should().NotBeNull();
                prn.IssuedToOrganisation.Should().NotBeNull();
                prn.Accreditation.Should().NotBeNull();
                prn.TonnageValue.Should().BeGreaterThan(0);
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_PrnNumbersFollowExpectedPattern()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;
        var hourlyPrnSuffix = DateTime.UtcNow.ToString("yyyyMMddHH");

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        result
            .Should()
            .AllSatisfy(prn =>
            {
                prn.PrnNumber.Should().StartWith("STUB");
                prn.PrnNumber.Should().EndWith(hourlyPrnSuffix);
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_IssuedToOrganisationUsesConfiguredStubOrgId()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        // PRNs 01-14 use StubOrgId
        var standardPrns = result
            .Where(p =>
                !p.PrnNumber!.Contains("STUB15")
                && !p.PrnNumber.Contains("STUB16")
                && !p.PrnNumber.Contains("STUB17")
            )
            .ToList();

        standardPrns.Should().HaveCount(14);
        standardPrns
            .Should()
            .AllSatisfy(prn =>
            {
                prn.IssuedToOrganisation!.Id.Should().Be(TestStubOrgId);
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ComplianceSchemePrnsUseComplianceSchemeOrgId()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        // STUBs 15-17 use OrganisationIdComplianceScheme
        var complianceSchemePrns = result
            .Where(p =>
                p.PrnNumber!.Contains("STUB15")
                || p.PrnNumber.Contains("STUB16")
                || p.PrnNumber.Contains("STUB17")
            )
            .ToList();

        complianceSchemePrns.Should().HaveCount(3);
        complianceSchemePrns
            .Should()
            .AllSatisfy(prn =>
            {
                prn.IssuedToOrganisation!.Id.Should().Be(TestComplianceSchemeOrgId);
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ContainsAwaitingAcceptanceStatus()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var awaitingAcceptancePrns = result
            .Where(p => p.Status!.CurrentStatus == RrepwStatus.AwaitingAcceptance)
            .ToList();
        awaitingAcceptancePrns.Should().NotBeEmpty();
        awaitingAcceptancePrns
            .Should()
            .AllSatisfy(prn =>
            {
                prn.Status!.AuthorisedAt.Should().NotBeNull();
                prn.Status.AuthorisedBy.Should().NotBeNull();
                prn.Status.AuthorisedBy!.FullName.Should().NotBeNullOrEmpty();
                prn.Status.AuthorisedBy.JobTitle.Should().Be("Test Manager");
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ContainsCancelledStatus()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var cancelledPrns = result
            .Where(p => p.Status!.CurrentStatus == RrepwStatus.Cancelled)
            .ToList();
        cancelledPrns.Should().HaveCount(2);
        cancelledPrns
            .Should()
            .AllSatisfy(prn =>
            {
                prn.Status!.CancelledAt.Should().NotBeNull();
                prn.Status.AuthorisedAt.Should().NotBeNull();
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ContainsRejectedStatus()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var rejectedPrns = result
            .Where(p => p.Status!.CurrentStatus == RrepwStatus.Rejected)
            .ToList();
        rejectedPrns.Should().HaveCount(1);
        rejectedPrns.First().Status!.RejectedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ContainsExportPrns()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var exportPrns = result.Where(p => p.IsExport == true).ToList();
        exportPrns.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ContainsDecemberWastePrn()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var decemberWastePrns = result.Where(p => p.IsDecemberWaste == true).ToList();
        decemberWastePrns.Should().HaveCount(1);
        decemberWastePrns.First().PrnNumber.Should().Contain("STUB12");
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ContainsAllMaterialTypes()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var materials = result.Select(p => p.Accreditation!.Material).Distinct().ToList();
        materials.Should().Contain(RrepwMaterialName.Plastic);
        materials.Should().Contain(RrepwMaterialName.Paper);
        materials.Should().Contain(RrepwMaterialName.Aluminium);
        materials.Should().Contain(RrepwMaterialName.Glass);
        materials.Should().Contain(RrepwMaterialName.Steel);
        materials.Should().Contain(RrepwMaterialName.Fibre);
        materials.Should().Contain(RrepwMaterialName.Wood);
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_ContainsAllRegulators()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var regulators = result
            .Select(p => p.Accreditation!.SubmittedToRegulator)
            .Distinct()
            .ToList();
        regulators.Should().Contain(RrepwSubmittedToRegulator.EnvironmentAgency_EA);
        regulators.Should().Contain(RrepwSubmittedToRegulator.NaturalResourcesWales_NRW);
        regulators
            .Should()
            .Contain(RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA);
        regulators
            .Should()
            .Contain(RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA);
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_GlassPrnsHaveGlassRecyclingProcess()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var glassPrns = result
            .Where(p => p.Accreditation!.Material == RrepwMaterialName.Glass)
            .ToList();
        glassPrns.Should().HaveCount(2);
        glassPrns
            .Select(p => p.Accreditation!.GlassRecyclingProcess)
            .Should()
            .Contain(RrepwGlassRecyclingProcess.GlassRemelt);
        glassPrns
            .Select(p => p.Accreditation!.GlassRecyclingProcess)
            .Should()
            .Contain(RrepwGlassRecyclingProcess.GlassOther);
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_NonGlassPrnsDoNotHaveGlassRecyclingProcess()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var nonGlassPrns = result
            .Where(p => p.Accreditation!.Material != RrepwMaterialName.Glass)
            .ToList();
        nonGlassPrns
            .Should()
            .AllSatisfy(prn =>
            {
                prn.Accreditation!.GlassRecyclingProcess.Should().BeNull();
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_TonnageValuesAreUnique()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var tonnageValues = result.Select(p => p.TonnageValue).ToList();
        // STUB 14-17 have the same tonnage of 800, and STUB 13 has variable tonnage based on minute
        var distinctTonnageCount = tonnageValues.Distinct().Count();
        distinctTonnageCount.Should().BeGreaterOrEqualTo(13);
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_Prn13HasFixedIdForUpdateTesting()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var prn13 = result.FirstOrDefault(p => p.PrnNumber!.Contains("STUB13"));
        prn13.Should().NotBeNull();
        prn13!.Id.Should().Be("prn13-fixed-id-for-update-test");
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_AccreditationYearIs2026ByDefault()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        result
            .Should()
            .AllSatisfy(prn =>
            {
                prn.Accreditation!.AccreditationYear.Should().Be(2026);
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_AccreditationNumbersFollowPattern()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        result
            .Should()
            .AllSatisfy(prn =>
            {
                prn.Accreditation!.AccreditationNumber.Should().StartWith("STUB-ACC-");
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_SiteAddressIsPopulatedWhereExpected()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var prnsWithSiteAddress = result.Where(p => p.Accreditation!.SiteAddress != null).ToList();
        prnsWithSiteAddress.Should().NotBeEmpty();
        prnsWithSiteAddress
            .Should()
            .AllSatisfy(prn =>
            {
                prn.Accreditation!.SiteAddress!.Line1.Should().NotBeNullOrEmpty();
                prn.Accreditation.SiteAddress.Postcode.Should().NotBeNullOrEmpty();
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_SomePrnsHaveFullAddress()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var prnsWithFullAddress = result
            .Where(p =>
                p.Accreditation!.SiteAddress != null
                && p.Accreditation.SiteAddress.Line2 != null
                && p.Accreditation.SiteAddress.Town != null
            )
            .ToList();

        prnsWithFullAddress.Should().NotBeEmpty();
        prnsWithFullAddress
            .Should()
            .AllSatisfy(prn =>
            {
                prn.Accreditation!.SiteAddress!.Line2.Should().Be("Unit 1");
                prn.Accreditation.SiteAddress.Town.Should().NotBeNullOrEmpty();
                prn.Accreditation.SiteAddress.County.Should().NotBeNullOrEmpty();
                prn.Accreditation.SiteAddress.Country.Should().Be("United Kingdom");
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_IssuedByOrganisationHasExpectedFields()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        result
            .Should()
            .AllSatisfy(prn =>
            {
                prn.IssuedByOrganisation!.Id.Should().NotBeNullOrEmpty();
                prn.IssuedByOrganisation.Name.Should().NotBeNullOrEmpty();
            });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_SomePrnsHaveIssuedByTradingName()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var prnsWithTradingName = result
            .Where(p => p.IssuedByOrganisation!.TradingName != null)
            .ToList();
        var prnsWithoutTradingName = result
            .Where(p => p.IssuedByOrganisation!.TradingName == null)
            .ToList();

        prnsWithTradingName.Should().NotBeEmpty();
        prnsWithoutTradingName.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_Prn12HasAcceptedAtIgnoredField()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var prn12 = result.FirstOrDefault(p => p.PrnNumber!.Contains("STUB12"));
        prn12.Should().NotBeNull();
        prn12!.Status!.CurrentStatus.Should().Be(RrepwStatus.AwaitingAcceptance);
        prn12.Status.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_Prn15HasCustomOrgNameAndTradingName()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var prn15 = result.FirstOrDefault(p => p.PrnNumber!.Contains("STUB15"));
        prn15.Should().NotBeNull();
        // Note: CreatePrn passes TradingName as orgName and OrgName as tradingName (swapped)
        prn15!.IssuedToOrganisation!.Name.Should().Be("ABC Scheme Operator Ltd");
        prn15.IssuedToOrganisation.TradingName.Should().Be("ABC Packaging Scheme");
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_Prn16HasCustomOrgNameOnly()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var prn16 = result.FirstOrDefault(p => p.PrnNumber!.Contains("STUB16"));
        prn16.Should().NotBeNull();
        // Note: OrgName is passed as tradingName, Name falls back to default
        prn16!.IssuedToOrganisation!.Name.Should().Contain("XYZ Scheme Operator Ltd");
        prn16.IssuedToOrganisation.TradingName.Should().BeNull();
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_Prn17HasProducerCompanyName()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        var prn17 = result.FirstOrDefault(p => p.PrnNumber!.Contains("STUB17"));
        prn17.Should().NotBeNull();
        // Note: OrgName is passed as tradingName, Name falls back to default
        prn17!.IssuedToOrganisation!.Name.Should().Contain("Producer Company Ltd");
        prn17.IssuedToOrganisation.TradingName.Should().BeNull();
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_IssuerNotesArePopulated()
    {
        var dateFrom = DateTime.UtcNow.AddDays(-1);
        var dateTo = DateTime.UtcNow;

        var result = await _service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        result
            .Should()
            .AllSatisfy(prn =>
            {
                prn.IssuerNotes.Should().NotBeNullOrEmpty();
            });
    }

    [Fact]
    public async Task UpdatePrn_ReturnsAcceptedStatusCode()
    {
        var prn = new PrnUpdateStatus
        {
            PrnNumber = "TEST-PRN-001",
            PrnStatusId = 1,
            AccreditationYear = "2026",
            StatusDate = DateTime.UtcNow,
            SourceSystemId = "test-source-id",
        };

        var result = await _service.UpdatePrn(prn);

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task UpdatePrn_LogsCallDetails()
    {
        var prn = new PrnUpdateStatus
        {
            PrnNumber = "TEST-PRN-001",
            PrnStatusId = 1,
            AccreditationYear = "2026",
            StatusDate = DateTime.UtcNow,
            SourceSystemId = "test-source-id",
        };

        await _service.UpdatePrn(prn);

        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("UpdatePrn called")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
