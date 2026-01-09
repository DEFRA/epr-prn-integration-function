using AutoFixture;
using AutoMapper;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Mappers;

public class RrepwMappersTests
{
    private readonly Fixture _fixture = new();
    private readonly IMapper _mapper = RrepwMappers.CreateMapper();

    public RrepwMappersTests()
    {
        _fixture.Register(() =>
            _fixture.Build<Organisation>().With(o => o.Id, Guid.NewGuid().ToString()).Create()
        );
        _fixture.Register(() =>
            _fixture
                .Build<Status>()
                .With(o => o.CurrentStatus, RrepwStatus.AwaitingAcceptance)
                .Create()
        );
        _fixture.Register(() =>
            _fixture
                .Build<Accreditation>()
                .With(o => o.Material, RrepwMaterialName.Aluminium)
                .With(o => o.GlassRecyclingProcess, (string?)null)
                .With(o => o.SubmittedToRegulator, RrepwSubmittedToRegulator.EnvironmentAgency_EA)
                .Create()
        );
        _fixture.Register(() =>
            _fixture
                .Build<Address>()
                .With(o => o.Line1, "123 Test Street")
                .With(o => o.Line2, (string?)null)
                .With(o => o.Town, (string?)null)
                .With(o => o.County, (string?)null)
                .With(o => o.Country, (string?)null)
                .With(o => o.Postcode, "TE5 7ST")
                .Create()
        );
    }

    private PackagingRecyclingNote CreatePackagingRecyclingNote()
    {
        return _fixture.Build<PackagingRecyclingNote>().Create();
    }

    [Theory]
    [InlineData(RrepwStatus.AwaitingAcceptance, EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData(RrepwStatus.Cancelled, EprnStatus.CANCELLED)]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_CurrentStatus(
        string status,
        EprnStatus expected
    )
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Status!.CurrentStatus = status;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.PrnStatusId.Should().Be((int)expected);
    }

    [Fact]
    public void ShouldMapPackagingRecyclingNoteToPrn_WithNulls()
    {
        var prn = new PackagingRecyclingNote();
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest
            .Should()
            .BeEquivalentTo(new SavePrnDetailsRequest { ObligationYear = "2026" });
    }

    [Theory]
    [InlineData(RrepwStatus.Accepted)]
    [InlineData(RrepwStatus.AwaitingAuthorisation)]
    [InlineData(RrepwStatus.AwaitingCancellation)]
    [InlineData(RrepwStatus.Rejected)]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_CurrentStatus_Wrong(string status)
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Status!.CurrentStatus = status;
        _mapper
            .Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn)
            .PrnStatusId.Should()
            .BeNull();
    }

    [Theory]
    [InlineData(RrepwMaterialName.Aluminium, null, RpdMaterialName.Aluminium)]
    [InlineData(RrepwMaterialName.Fibre, null, RpdMaterialName.Fibre)]
    [InlineData(
        RrepwMaterialName.Glass,
        RrepwGlassRecyclingProcess.GlassOther,
        RpdMaterialName.GlassOther
    )]
    [InlineData(
        RrepwMaterialName.Glass,
        RrepwGlassRecyclingProcess.GlassRemelt,
        RpdMaterialName.GlassRemelt
    )]
    [InlineData(RrepwMaterialName.Paper, null, RpdMaterialName.PaperBoard)]
    [InlineData(RrepwMaterialName.Plastic, null, RpdMaterialName.Plastic)]
    [InlineData(RrepwMaterialName.Steel, null, RpdMaterialName.Steel)]
    [InlineData(RrepwMaterialName.Wood, null, RpdMaterialName.Wood)]
    public void ShouldMapPackagingRecyclingNoteToPrn_MaterialName(
        string materialName,
        string? glassRecyclingProcess,
        string expectedMaterialName
    )
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.Material = materialName;
        prn.Accreditation.GlassRecyclingProcess = glassRecyclingProcess;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.MaterialName.Should().Be(expectedMaterialName);
    }

    [Fact]
    public void ShouldMapPackagingRecyclingNoteToPrn_MaterialName_Invalid()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.Material = "invalidMaterialName";
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.MaterialName.Should().BeNull();
    }

    [Fact]
    public void ShouldMapPackagingRecyclingNoteToPrn_GlassRecyclingProcess_Invalid()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.Material = RrepwMaterialName.Glass;
        prn.Accreditation.GlassRecyclingProcess = "invalidProcess";
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.MaterialName.Should().BeNull();
    }

    [Theory]
    [InlineData(RrepwMaterialName.Aluminium, RpdProcesses.R4)]
    [InlineData(RrepwMaterialName.Fibre, RpdProcesses.R3)]
    [InlineData(RrepwMaterialName.Glass, RpdProcesses.R5)]
    [InlineData(RrepwMaterialName.Paper, RpdProcesses.R3)]
    [InlineData(RrepwMaterialName.Plastic, RpdProcesses.R3)]
    [InlineData(RrepwMaterialName.Steel, RpdProcesses.R4)]
    [InlineData(RrepwMaterialName.Wood, RpdProcesses.R3)]
    public void ShouldMapPackagingRecyclingNoteToPrn_ProcessToBeUsed(
        string materialName,
        string expectedProcessToBeUsed
    )
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.Material = materialName;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.ProcessToBeUsed.Should().Be(expectedProcessToBeUsed);
    }

    [Fact]
    public void ShouldMapPackagingRecyclingNoteToPrn_ProcessToBeUsed_Invalid()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.Material = "invalidMaterialName";
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.ProcessToBeUsed.Should().BeNull();
    }

    [Theory]
    [InlineData(
        RrepwSubmittedToRegulator.EnvironmentAgency_EA,
        RpdReprocessorExporterAgency.EnvironmentAgency
    )]
    [InlineData(
        RrepwSubmittedToRegulator.NaturalResourcesWales_NRW,
        RpdReprocessorExporterAgency.NaturalResourcesWales
    )]
    [InlineData(
        RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA,
        RpdReprocessorExporterAgency.NorthernIrelandEnvironmentAgency
    )]
    [InlineData(
        RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA,
        RpdReprocessorExporterAgency.ScottishEnvironmentProtectionAgency
    )]
    public void ShouldMapPackagingRecyclingNoteToPrn_SubmittedToRegulator(
        string sourceStr,
        string expectedStr
    )
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SubmittedToRegulator = sourceStr;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.ReprocessorExporterAgency.Should().Be(expectedStr);
    }

    [Fact]
    public void ShouldMapPackagingRecyclingNoteToPrn_SubmittedToRegulator_Invalid()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SubmittedToRegulator = "invalidRegulator";
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.ReprocessorExporterAgency.Should().BeNull();
    }

    [Theory]
    [InlineData(RrepwStatus.AwaitingAcceptance)]
    [InlineData(RrepwStatus.Cancelled)]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_AuthorizedAt(string status)
    {
        var adt = new DateTime(2024, 12, 08);
        var cdt = new DateTime(2024, 12, 08);
        var prn = CreatePackagingRecyclingNote();
        prn.Status!.CurrentStatus = status;
        prn.Status.AuthorisedAt = adt;
        prn.Status.CancelledAt = cdt;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        switch (status)
        {
            case RrepwStatus.Cancelled:
                savePrnDetailsRequest.StatusUpdatedOn.Should().Be(cdt);
                savePrnDetailsRequest.IssueDate.Should().Be(null);
                break;
            case RrepwStatus.AwaitingAcceptance:
                savePrnDetailsRequest.StatusUpdatedOn.Should().Be(adt);
                savePrnDetailsRequest.IssueDate.Should().Be(adt);
                break;
            default:
                Assert.Fail("Unexpected status");
                break;
        }
    }

    [Theory]
    [InlineData(RrepwStatus.Accepted)]
    [InlineData(RrepwStatus.AwaitingAuthorisation)]
    [InlineData(RrepwStatus.AwaitingCancellation)]
    [InlineData(RrepwStatus.Rejected)]
    [InlineData("InvalidStatus")]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_AuthorizedAt_Invalid(string status)
    {
        var adt = new DateTime(2024, 12, 08);
        var cdt = new DateTime(2024, 12, 08);
        var prn = CreatePackagingRecyclingNote();
        prn.Status!.CurrentStatus = status;
        prn.Status.AuthorisedAt = adt;
        prn.Status.CancelledAt = cdt;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.StatusUpdatedOn.Should().BeNull();
    }

    [Fact]
    public void ShouldMapPackagingRecyclingNoteToPrn_TheRest()
    {
        var prn = CreatePackagingRecyclingNote();
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.SourceSystemId.Should().Be(prn.Id);
        savePrnDetailsRequest.PrnNumber.Should().Be(prn.PrnNumber);
        savePrnDetailsRequest.PrnSignatory.Should().Be(prn.Status!.AuthorisedBy!.FullName);
        savePrnDetailsRequest.PrnSignatoryPosition.Should().Be(prn.Status.AuthorisedBy.JobTitle);
        savePrnDetailsRequest.IssuedByOrg.Should().Be(prn.IssuedByOrganisation!.Name);
        savePrnDetailsRequest.OrganisationId.Should().Be(prn.IssuedToOrganisation!.Id);
        savePrnDetailsRequest.OrganisationName.Should().Be(prn.IssuedToOrganisation.Name);
        savePrnDetailsRequest
            .AccreditationNumber.Should()
            .Be(prn.Accreditation!.AccreditationNumber);
        savePrnDetailsRequest
            .AccreditationYear.Should()
            .Be(prn.Accreditation.AccreditationYear.ToString());
        savePrnDetailsRequest
            .ReprocessingSite.Should()
            .Be(
                $"{prn.Accreditation.SiteAddress!.Line1}, {prn.Accreditation.SiteAddress!.Postcode}"
            );
        savePrnDetailsRequest.DecemberWaste.Should().Be(prn.IsDecemberWaste);
        savePrnDetailsRequest.IsExport.Should().Be(prn.IsExport);
        savePrnDetailsRequest.TonnageValue.Should().Be(prn.TonnageValue);
        savePrnDetailsRequest.IssuerNotes.Should().Be(prn.IssuerNotes);
        savePrnDetailsRequest.ObligationYear.Should().Be("2026");
    }

    [Fact]
    public void ShouldGetReprocessingSite()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SiteAddress = new Address
        {
            Line1 = "Site Line 1",
            Line2 = "Site Line 2",
            Town = "Site Town",
            County = "Site County",
            Postcode = "S1 1SS",
            Country = "Site Country",
        };
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be("Site Line 1, Site Line 2, Site Town, Site County, S1 1SS, Site Country");
    }

    [Fact]
    public void ShouldGetReprocessingSiteTrim()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SiteAddress = new Address
        {
            Line1 = "  Site Line 1  ",
            Line2 = "  Site Line 2  ",
            Town = "  Site Town  ",
            County = "  Site County  ",
            Postcode = "  S1 1SS  ",
            Country = " Site Country  ",
        };
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be("Site Line 1, Site Line 2, Site Town, Site County, S1 1SS, Site Country");
    }

    [Fact]
    public void ShouldGetReprocessingSite_NotAllSet()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SiteAddress = new Address
        {
            Line1 = "Site Line 1",
            Line2 = "Site Line 2",
            Postcode = "S1 1SS",
        };
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be("Site Line 1, Site Line 2, S1 1SS");
    }

    [Fact]
    public void ShouldGetReprocessingSite_AllNull()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SiteAddress = new Address
        {
            Line1 = null,
            Line2 = null,
            Town = null,
            County = null,
            Postcode = null,
            Country = null,
        };
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be("");
    }

    [Fact]
    public void ShouldGetReprocessingSite_Whitespace()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SiteAddress = new Address
        {
            Line1 = " ",
            Line2 = " ",
            Town = " ",
            County = " ",
            Postcode = " ",
            Country = " ",
        };
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be("");
    }

    [Fact]
    public void ShouldGetReprocessingSite_Empty()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SiteAddress = new Address
        {
            Line1 = "",
            Line2 = "",
            Town = "",
            County = "",
            Postcode = "",
            Country = "",
        };
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be("");
    }

    [Fact]
    public void ShouldGetReprocessingSite_Mix()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SiteAddress = new Address
        {
            Line1 = "a      ",
            Line2 = "  ",
            Town = "",
            County = "",
            Postcode = "     b       ",
            Country = "     ",
        };
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be("a, b");
    }

    [Fact]
    public void ShouldGetReprocessingSite_AddressNull()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation!.SiteAddress = null;
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be(null);
    }

    [Fact]
    public void ShouldGetReprocessingSite_AccreditationNull()
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation = null;
        var site = RrepwMappers.GetReprocessingSite(prn);
        site.Should().Be(null);
    }
}
