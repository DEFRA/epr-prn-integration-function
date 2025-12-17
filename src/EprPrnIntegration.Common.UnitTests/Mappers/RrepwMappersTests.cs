using AutoFixture;
using AutoMapper;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Mappers;

public class RrepwMappersTests
{
    private readonly Fixture _fixture = new();
    private readonly IMapper _mapper = RrepwMappers.CreateMapper();


    [Fact]
    public void ShouldMapPackagingRecyclingNoteToPrn_TheRest()
    {
        var prn = _fixture.Create<PackagingRecyclingNote>();
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.SourceSystemId.Should().Be(prn.Id);
        savePrnDetailsRequest.EvidenceNo.Should().Be(prn.PrnNumber);
        savePrnDetailsRequest.PrnSignatory.Should().Be(prn.Status.AuthorisedBy!.FullName);
        savePrnDetailsRequest.PrnSignatoryPosition.Should().Be(prn.Status.AuthorisedBy.JobTitle);
    }

    [Theory]
    [InlineData("accepted", EprnStatus.ACCEPTED)]
    [InlineData("awaiting_acceptance", EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData("awaiting_authorisation", EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData("awaiting_cancellation", EprnStatus.CANCELLED)]
    [InlineData("cancelled", EprnStatus.CANCELLED)]
    [InlineData("rejected", EprnStatus.REJECTED)]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_CurrentStatus(string status, EprnStatus expected)
    {
        var prn = _fixture.Create<PackagingRecyclingNote>();
        prn.Status.CurrentStatus = status;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        savePrnDetailsRequest.EvidenceStatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("accepted")]
    [InlineData("awaiting_acceptance")]
    [InlineData("awaiting_authorisation")]
    [InlineData("awaiting_cancellation")]
    [InlineData("cancelled")]
    [InlineData("rejected")]
    public void ShouldMapPackagingRecyclingNoteToPrn_Status_AuthorizedAt(string status)
    {
        var adt = new DateTime(2024, 12, 08);
        var cdt = new DateTime(2024, 12, 08);
        var prn = _fixture.Create<PackagingRecyclingNote>();
        prn.Status.CurrentStatus = status;
        prn.Status.AuthorisedAt = adt;
        prn.Status.CancelledAt = cdt;
        var savePrnDetailsRequest = _mapper.Map<PackagingRecyclingNote, SavePrnDetailsRequest>(prn);
        switch (status)
        {
            case "cancelled":
                savePrnDetailsRequest.CancelledDate.Should().Be(cdt);
                savePrnDetailsRequest.StatusDate.Should().BeNull();
                break;
            case "accepted":
                savePrnDetailsRequest.CancelledDate.Should().BeNull();
                savePrnDetailsRequest.StatusDate.Should().Be(adt);
                break;
            default:
                savePrnDetailsRequest.CancelledDate.Should().BeNull();
                savePrnDetailsRequest.StatusDate.Should().BeNull();
                break;
        }
    }
}