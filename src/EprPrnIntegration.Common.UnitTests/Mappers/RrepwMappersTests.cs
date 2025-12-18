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
                .With(o => o.CurrentStatus, StatusName.AwaitingAcceptance)
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
    }

    private PackagingRecyclingNote CreatePackagingRecyclingNote()
    {
        return _fixture.Build<PackagingRecyclingNote>().Create();
    }

    [Theory]
    [InlineData(StatusName.Accepted, EprnStatus.ACCEPTED)]
    [InlineData(StatusName.AwaitingAcceptance, EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData(StatusName.AwaitingAuthorisation, EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData(StatusName.AwaitingCancellation, EprnStatus.CANCELLED)]
    [InlineData(StatusName.Cancelled, EprnStatus.CANCELLED)]
    [InlineData(StatusName.Rejected, EprnStatus.REJECTED)]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_CurrentStatus(
        string status,
        EprnStatus expected
    )
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Status.CurrentStatus = status;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequestV2>(
            prn
        );
        savePrnDetailsRequest.PrnStatusId.Should().Be((int)expected);
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
        prn.Accreditation.Material = materialName;
        prn.Accreditation.GlassRecyclingProcess = glassRecyclingProcess;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequestV2>(
            prn
        );
        savePrnDetailsRequest.MaterialName.Should().Be(expectedMaterialName);
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
        prn.Accreditation.Material = materialName;
        prn.Accreditation.GlassRecyclingProcess = RrepwGlassRecyclingProcess.GlassOther;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequestV2>(
            prn
        );
        savePrnDetailsRequest.ProcessToBeUsed.Should().Be(expectedProcessToBeUsed);
    }

    [Theory]
    [InlineData(
        RrepwSubmittedToRegulator.EnvironmentAgency_EA,
        RpdSubmittedToRegulator.EnvironmentAgency
    )]
    [InlineData(
        RrepwSubmittedToRegulator.NaturalResourcesWales_NRW,
        RpdSubmittedToRegulator.NaturalResourcesWales
    )]
    [InlineData(
        RrepwSubmittedToRegulator.NorthernIrelandEnvironmentAgency_SEPA,
        RpdSubmittedToRegulator.NorthernIrelandEnvironmentAgency
    )]
    [InlineData(
        RrepwSubmittedToRegulator.ScottishEnvironmentProtectionAge_NIEA,
        RpdSubmittedToRegulator.ScottishEnvironmentProtectionAge
    )]
    public void ShouldMapPackagingRecyclingNoteToPrn_SubmittedToRegulator(
        string sourceStr,
        string expectedStr
    )
    {
        var prn = CreatePackagingRecyclingNote();
        prn.Accreditation.SubmittedToRegulator = sourceStr;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequestV2>(
            prn
        );
        savePrnDetailsRequest.ReprocessorExporterAgency.Should().Be(expectedStr);
    }

    [Theory]
    [InlineData(StatusName.AwaitingAcceptance)]
    [InlineData(StatusName.Cancelled)]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_AuthorizedAt(string status)
    {
        var adt = new DateTime(2024, 12, 08);
        var cdt = new DateTime(2024, 12, 08);
        var prn = CreatePackagingRecyclingNote();
        prn.Status.CurrentStatus = status;
        prn.Status.AuthorisedAt = adt;
        prn.Status.CancelledAt = cdt;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequestV2>(
            prn
        );
        switch (status)
        {
            case StatusName.Cancelled:
                savePrnDetailsRequest.StatusUpdatedOn.Should().Be(cdt);
                break;
            case StatusName.AwaitingAcceptance:
                savePrnDetailsRequest.StatusUpdatedOn.Should().Be(adt);
                break;
            default:
                Assert.Fail("Unexpected status");
                break;
        }
    }

    [Theory]
    [InlineData(StatusName.Accepted)]
    [InlineData(StatusName.AwaitingAuthorisation)]
    [InlineData(StatusName.AwaitingCancellation)]
    [InlineData(StatusName.Rejected)]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_AuthorizedAt_Wrong(string status)
    {
        var adt = new DateTime(2024, 12, 08);
        var cdt = new DateTime(2024, 12, 08);
        var prn = CreatePackagingRecyclingNote();
        prn.Status.CurrentStatus = status;
        prn.Status.AuthorisedAt = adt;
        prn.Status.CancelledAt = cdt;
        Assert.Throws<AutoMapperMappingException>(() =>
            _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequestV2>(prn)
        );
    }

    [Fact]
    public void ShouldMapPackagingRecyclingNoteToPrn_TheRest()
    {
        var prn = CreatePackagingRecyclingNote();
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequestV2>(
            prn
        );
        savePrnDetailsRequest.SourceSystemId.Should().Be(prn.Id);
        savePrnDetailsRequest.PrnNumber.Should().Be(prn.PrnNumber);
        savePrnDetailsRequest.PrnSignatory.Should().Be(prn.Status.AuthorisedBy!.FullName);
        savePrnDetailsRequest.PrnSignatoryPosition.Should().Be(prn.Status.AuthorisedBy.JobTitle);
        savePrnDetailsRequest.IssuedByOrg.Should().Be(prn.IssuedByOrganisation.Name);
        savePrnDetailsRequest.OrganisationId.Should().Be(prn.IssuedToOrganisation.Id);
        savePrnDetailsRequest.OrganisationName.Should().Be(prn.IssuedToOrganisation.Name);
        savePrnDetailsRequest
            .AccreditationNumber.Should()
            .Be(prn.Accreditation.AccreditationNumber);
        savePrnDetailsRequest
            .AccreditationYear.Should()
            .Be(prn.Accreditation.AccreditationYear.ToString());
        savePrnDetailsRequest.ReprocessingSite.Should().Be(prn.Accreditation.SiteAddress!.Line1);
        savePrnDetailsRequest.DecemberWaste.Should().Be(prn.IsDecemberWaste);
        savePrnDetailsRequest.IsExport.Should().Be(prn.IsExport);
        savePrnDetailsRequest.TonnageValue.Should().Be(prn.TonnageValue);
        savePrnDetailsRequest.IssuerNotes.Should().Be(prn.IssuerNotes);
        savePrnDetailsRequest.ObligationYear.Should().Be("2026");
    }
}
