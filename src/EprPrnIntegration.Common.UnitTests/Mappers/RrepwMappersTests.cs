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
    public void ShouldMapRrepwPrnToPrn_TheRest()
    {
        var rrepwPrn = _fixture.Create<RrepwPrn>();
        var savePrnDetailsRequest = _mapper.Map<RrepwPrn, SavePrnDetailsRequest>(rrepwPrn);
        savePrnDetailsRequest.SourceSystemId.Should().Be(rrepwPrn.Id);
        savePrnDetailsRequest.EvidenceNo.Should().Be(rrepwPrn.PrnNumber);
        savePrnDetailsRequest.PrnSignatory.Should().Be(rrepwPrn.Status.AuthorisedBy.FullName);
        savePrnDetailsRequest.PrnSignatoryPosition.Should().Be(rrepwPrn.Status.AuthorisedBy.JobTitle);
    }

    [Theory]
    [InlineData(NoteStatus.accepted, EprnStatus.ACCEPTED)]
    [InlineData(NoteStatus.awaiting_acceptance, EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData(NoteStatus.awaiting_authorisation, EprnStatus.AWAITINGACCEPTANCE)]
    [InlineData(NoteStatus.awaiting_cancellation, EprnStatus.CANCELLED)]
    [InlineData(NoteStatus.cancelled, EprnStatus.CANCELLED)]
    [InlineData(NoteStatus.rejected, EprnStatus.REJECTED)]
    public void ShouldMapRrepwPrnToPrn_Status_CurrentStatus(NoteStatus status, EprnStatus expected)
    {
        var rrepwPrn = _fixture.Create<RrepwPrn>();
        rrepwPrn.Status.CurrentStatus = status;
        var savePrnDetailsRequest = _mapper.Map<RrepwPrn, SavePrnDetailsRequest>(rrepwPrn);
        savePrnDetailsRequest.EvidenceStatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData(NoteStatus.accepted)]
    [InlineData(NoteStatus.awaiting_acceptance)]
    [InlineData(NoteStatus.awaiting_authorisation)]
    [InlineData(NoteStatus.awaiting_cancellation)]
    [InlineData(NoteStatus.cancelled)]
    [InlineData(NoteStatus.rejected)]
    public void ShouldMapRrepwPrnToPrn_Status_AuthorizedAt(NoteStatus status)
    {
        var adt = new DateTime(2024, 12, 08);
        var cdt = new DateTime(2024, 12, 08);
        var rrepwPrn = _fixture.Create<RrepwPrn>();
        rrepwPrn.Status.CurrentStatus = status;
        rrepwPrn.Status.AuthorisedAt = adt;
        rrepwPrn.Status.CancelledAt = cdt;
        var savePrnDetailsRequest = _mapper.Map<RrepwPrn, SavePrnDetailsRequest>(rrepwPrn);
        switch (status)
        {
            case NoteStatus.cancelled:
                savePrnDetailsRequest.CancelledDate.Should().Be(cdt);
                savePrnDetailsRequest.StatusDate.Should().BeNull();
                break;
            case NoteStatus.accepted:
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