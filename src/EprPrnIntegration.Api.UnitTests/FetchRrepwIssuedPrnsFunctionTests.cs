using System.Globalization;
using System.Net;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Api.UnitTests.Helpers;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests;

public class FetchRrepwIssuedPrnsFunctionTests
{
    private readonly Mock<ILogger<FetchRrepwIssuedPrnsFunction>> _loggerMock = new();
    private readonly Mock<ILastUpdateService> _lastUpdateServiceMock = new();
    private readonly Mock<IRrepwService> _rrepwServiceMock = new();
    private readonly Mock<IPrnService> _prnServiceMock = new();
    private readonly IOptions<FetchRrepwIssuedPrnsConfiguration> _config = Options.Create(
        new FetchRrepwIssuedPrnsConfiguration { DefaultStartDate = "2024-01-01" }
    );

    private readonly FetchRrepwIssuedPrnsFunction _function;

    public FetchRrepwIssuedPrnsFunctionTests()
    {
        _function = new(
            _lastUpdateServiceMock.Object,
            _loggerMock.Object,
            _rrepwServiceMock.Object,
            _prnServiceMock.Object,
            _config
        );
    }

    [Fact]
    public async Task ProcessesMultiplePrns()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002"),
            CreatePrn("PRN-003"),
        };

        SetupSavePrns(prns);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x =>
                x.ListPackagingRecyclingNotes(
                    ItEx.IsCloseTo(DateTime.MinValue),
                    ItEx.IsCloseTo(DateTime.UtcNow)
                )
            )
            .ReturnsAsync(prns);

        await _function.Run(new TimerInfo());

        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-001")),
            Times.Once
        );
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002")),
            Times.Once
        );
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-003")),
            Times.Once
        );
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    [Fact]
    public async Task WhenRrepwPrnServiceThrows_DoesNotProcessPrns()
    {
        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);
        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        await Assert.ThrowsAsync<HttpRequestException>(() => _function.Run(new TimerInfo()));

        // Verify no PRNs were saved
        _prnServiceMock.Verify(x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>()), Times.Never);
        // Verify last update was NOT set
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    [Fact]
    public async Task WhenRrepwPrnServiceReturnsZeroItems_DoesNotSetLastUpdateTime()
    {
        var emptyPrnsList = new List<PackagingRecyclingNote>();

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);
        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(emptyPrnsList);

        await _function.Run(new TimerInfo());

        // Verify no PRNs were saved
        _prnServiceMock.Verify(x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>()), Times.Never);
        // Verify last update was NOT set
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    [Fact]
    public async Task WhenOnePrnSaveFails_ContinuesProcessingAndUpdatesLastUpdatedTime()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-003"),
            CreatePrn("PRN-002-fails"),
        };
        SetupSavePrns(prns.Take(2));
        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x =>
                x.ListPackagingRecyclingNotes(
                    ItEx.IsCloseTo(DateTime.MinValue),
                    ItEx.IsCloseTo(DateTime.UtcNow)
                )
            )
            .ReturnsAsync(prns);
        _prnServiceMock
            .Setup(x =>
                x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002-fails"))
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await _function.Run(new TimerInfo());

        // Verify all three PRNs were attempted
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-001")),
            Times.Once
        );
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002-fails")),
            Times.Once
        );
        _prnServiceMock.Verify(
            x => x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-003")),
            Times.Once
        );

        // Verify last update WAS still set despite one failure
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task WhenTransientErrorOccurs_ForPrnService_RethrowsExceptionAndDoesNotUpdateLastUpdatedTime(
        HttpStatusCode statusCode
    )
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002-transient"),
        };

        SetupSavePrns(prns.Take(1));

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);
        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        _prnServiceMock
            .Setup(x =>
                x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002-transient"))
            )
            .ReturnsAsync(new HttpResponseMessage(statusCode));

        // Act & Assert - expect the exception to be rethrown
        await Assert.ThrowsAsync<ServiceException>(() => _function.Run(new TimerInfo()));

        // Verify last update was NOT set when transient error occurs
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    private void SetupSavePrns(IEnumerable<PackagingRecyclingNote> prns)
    {
        foreach (var prn in prns)
        {
            _prnServiceMock
                .Setup(x =>
                    x.SavePrn(It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == prn.PrnNumber))
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));
        }
    }

    [Fact]
    public async Task WhenNoBlobStorageValueExists_UsesDefaultStartDateFromConfiguration()
    {
        var prns = new List<PackagingRecyclingNote> { CreatePrn("PRN-001"), CreatePrn("PRN-002") };
        SetupSavePrns(prns);
        var expectedStartDate = DateTime.SpecifyKind(
            DateTime.ParseExact(
                _config.Value.DefaultStartDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            ),
            DateTimeKind.Utc
        );

        // Setup: GetLastUpdate returns null (no blob storage value)
        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync((DateTime?)null);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(expectedStartDate, It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        await _function.Run(new TimerInfo());

        // Verify GetLastUpdate was called
        _lastUpdateServiceMock.Verify(
            x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns),
            Times.Once
        );

        // Verify PRNs were processed
        _prnServiceMock.Verify(x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>()), Times.Exactly(2));

        // Verify last update was set
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    [Fact]
    public async Task StubbedRrepwService_WorksWithMappersAndFunction()
    {
        // Arrange - use concrete StubbedRrepwService instead of mock
        var stubbedRrepwService = new StubbedRrepwService(Mock.Of<ILogger<StubbedRrepwService>>());

        var lastUpdateServiceMock = new Mock<ILastUpdateService>();
        var prnServiceMock = new Mock<IPrnService>();

        lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(It.IsAny<string>()))
            .ReturnsAsync(DateTime.MinValue);

        prnServiceMock
            .Setup(x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req =>
                        req.PrnNumber == "STUB-12345"
                        && req.AccreditationYear == "2026"
                        && req.MaterialName != null
                        && req.ProcessToBeUsed != null
                    )
                )
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));
        var function = new FetchRrepwIssuedPrnsFunction(
            lastUpdateServiceMock.Object,
            Mock.Of<ILogger<FetchRrepwIssuedPrnsFunction>>(),
            stubbedRrepwService,
            prnServiceMock.Object,
            _config
        );

        // Act
        await function.Run(new TimerInfo());

        // Verify last update was set
        lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Once
        );
    }

    private static PackagingRecyclingNote CreatePrn(
        string evidenceNo,
        string accreditationNo = "ACC-001",
        int accreditationYear = 2025,
        string material = "Plastic",
        int tonnes = 100
    )
    {
        return new PackagingRecyclingNote
        {
            Id = Guid.NewGuid().ToString(),
            PrnNumber = evidenceNo,
            Status = new Status
            {
                CurrentStatus = "ACTIVE",
                AuthorisedAt = DateTime.UtcNow.AddDays(-30),
            },
            IssuedByOrganisation = new Organisation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Issuer Organization",
            },
            IssuedToOrganisation = new Organisation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Recipient Organization",
            },
            Accreditation = new Accreditation
            {
                Id = Guid.NewGuid().ToString(),
                AccreditationNumber = accreditationNo,
                AccreditationYear = accreditationYear,
                Material = material,
                SubmittedToRegulator = "EA",
            },
            IsDecemberWaste = false,
            IsExport = false,
            TonnageValue = tonnes,
            IssuerNotes = "Test PRN",
        };
    }
}
