using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwPrnService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests;

public class UpdateRrepwPrnsFunctionTests
{
    private readonly Mock<ILogger<UpdateRrepwPrnsFunction>> _loggerMock = new();
    private readonly Mock<ILastUpdateService> _lastUpdateServiceMock = new();
    private readonly Mock<IRrepwPrnService> _rrepwPrnServiceMock = new();
    private readonly Mock<IPrnServiceV2> _prnServiceMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly IOptions<UpdateRrepwPrnsConfiguration> _config = Options.Create(new UpdateRrepwPrnsConfiguration { DefaultStartDate = "2024-01-01" });

    private readonly UpdateRrepwPrnsFunction _function;

    public UpdateRrepwPrnsFunctionTests()
    {
        _function = new(_lastUpdateServiceMock.Object, _loggerMock.Object, _rrepwPrnServiceMock.Object, _prnServiceMock.Object, _configurationMock.Object, _config);
    }

    [Fact]
    public async Task ProcessesMultiplePrns()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002"),
            CreatePrn("PRN-003")
        };

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _rrepwPrnServiceMock.Setup(x => x.GetPrns(It.IsAny<CancellationToken>()))
            .ReturnsAsync(prns);

        await _function.Run(new TimerInfo());

        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequestV2>(req => req.PrnNumber == "PRN-001")),
            Times.Once);
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequestV2>(req => req.PrnNumber == "PRN-002")),
            Times.Once);
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequestV2>(req => req.PrnNumber == "PRN-003")),
            Times.Once);
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task WhenRrepwPrnServiceThrows_DoesNotProcessPrns()
    {
        var expectedException = new HttpRequestException("Service unavailable");

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _rrepwPrnServiceMock.Setup(x => x.GetPrns(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        await Assert.ThrowsAsync<HttpRequestException>(() => _function.Run(new TimerInfo()));

        // Verify no PRNs were saved
        _prnServiceMock.Verify(x => x.SavePrn(It.IsAny<SavePrnDetailsRequestV2>()), Times.Never);
        // Verify last update was NOT set
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task WhenRrepwPrnServiceReturnsZeroItems_DoesNotSetLastUpdateTime()
    {
        var emptyPrnsList = new List<PackagingRecyclingNote>();

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _rrepwPrnServiceMock.Setup(x => x.GetPrns(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPrnsList);

        await _function.Run(new TimerInfo());

        // Verify no PRNs were saved
        _prnServiceMock.Verify(x => x.SavePrn(It.IsAny<SavePrnDetailsRequestV2>()), Times.Never);
        // Verify last update was NOT set
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task WhenOnePrnSaveFails_ContinuesProcessingAndUpdatesLastUpdatedTime()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002-fails"),
            CreatePrn("PRN-003")
        };

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _rrepwPrnServiceMock.Setup(x => x.GetPrns(It.IsAny<CancellationToken>()))
            .ReturnsAsync(prns);

        _prnServiceMock
            .Setup(x => x.SavePrn(It.Is<SavePrnDetailsRequestV2>(req => req.PrnNumber == "PRN-002-fails")))
            .ThrowsAsync(new HttpRequestException("Missing credentials", null, HttpStatusCode.Unauthorized));

        await _function.Run(new TimerInfo());

        // Verify all three PRNs were attempted
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequestV2>(req => req.PrnNumber == "PRN-001")),
            Times.Once);
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequestV2>(req => req.PrnNumber == "PRN-002-fails")),
            Times.Once);
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequestV2>(req => req.PrnNumber == "PRN-003")),
            Times.Once);

        // Verify last update WAS still set despite one failure
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task WhenTransientErrorOccurs_ForPrnService_RethrowsExceptionAndDoesNotUpdateLastUpdatedTime(HttpStatusCode statusCode)
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002-transient")
        };

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _rrepwPrnServiceMock.Setup(x => x.GetPrns(It.IsAny<CancellationToken>()))
            .ReturnsAsync(prns);

        _prnServiceMock
            .Setup(x => x.SavePrn(It.Is<SavePrnDetailsRequestV2>(req => req.PrnNumber == "PRN-002-transient")))
            .ThrowsAsync(new HttpRequestException($"Error: {statusCode}", null, statusCode));

        // Act & Assert - expect the exception to be rethrown
        await Assert.ThrowsAsync<HttpRequestException>(() => _function.Run(new TimerInfo()));

        // Verify last update was NOT set when transient error occurs
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task WhenNoBlobStorageValueExists_UsesDefaultStartDateFromConfiguration()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002")
        };

        // Setup: GetLastUpdate returns null (no blob storage value)
        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync((DateTime?)null);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _rrepwPrnServiceMock.Setup(x => x.GetPrns(It.IsAny<CancellationToken>()))
            .ReturnsAsync(prns);

        await _function.Run(new TimerInfo());

        // Verify GetLastUpdate was called
        _lastUpdateServiceMock.Verify(x => x.GetLastUpdate("UpdateRrepwPrns"), Times.Once);

        // Verify PRNs were processed
        _prnServiceMock.Verify(
            x => x.SavePrn(It.IsAny<SavePrnDetailsRequestV2>()),
            Times.Exactly(2));

        // Verify last update was set
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    private static PackagingRecyclingNote CreatePrn(
        string evidenceNo,
        string accreditationNo = "ACC-001",
        int accreditationYear = 2025,
        string material = "Plastic",
        int tonnes = 100)
    {
        return new PackagingRecyclingNote
        {
            Id = Guid.NewGuid().ToString(),
            PrnNumber = evidenceNo,
            Status = new Status
            {
                CurrentStatus = "ACTIVE",
                AuthorisedAt = DateTime.UtcNow.AddDays(-30)
            },
            IssuedByOrganisation = new Organisation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Issuer Organization"
            },
            IssuedToOrganisation = new Organisation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Recipient Organization"
            },
            Accreditation = new Accreditation
            {
                Id = Guid.NewGuid().ToString(),
                AccreditationNumber = accreditationNo,
                AccreditationYear = accreditationYear,
                Material = material,
                SubmittedToRegulator = "EA"
            },
            IsDecemberWaste = false,
            IsExport = false,
            TonnageValue = tonnes,
            IssuerNotes = "Test PRN"
        };
    }
}
