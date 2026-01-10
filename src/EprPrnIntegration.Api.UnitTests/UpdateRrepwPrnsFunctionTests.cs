using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Api.UnitTests.Helpers;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.Service;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests;

public class UpdateRrepwPrnsFunctionTests
{
    private readonly Mock<ILogger<UpdateRrepwPrnsFunction>> _loggerMock = new();
    private readonly Mock<ILastUpdateService> _lastUpdateServiceMock = new();
    private readonly Mock<IRrepwService> _rrepwServiceMock = new();
    private readonly Mock<IPrnService> _prnServiceMock = new();
    private readonly IOptions<UpdateRrepwPrnsConfiguration> _config = Options.Create(
        new UpdateRrepwPrnsConfiguration { DefaultStartDate = "2024-01-01" }
    );

    private readonly UpdateRrepwPrnsFunction _function;
    private readonly Fixture _fixture = new();

    public UpdateRrepwPrnsFunctionTests()
    {
        _function = new UpdateRrepwPrnsFunction(
            _lastUpdateServiceMock.Object,
            _prnServiceMock.Object,
            _rrepwServiceMock.Object,
            _loggerMock.Object,
            _config
        );
    }

    private PrnUpdateStatus CreatePrnUpdateStatus(string prnId)
    {
        return _fixture
            .Build<PrnUpdateStatus>()
            .With(p => p.PrnNumber, prnId)
            .With(p => p.StatusDate, DateTime.UtcNow.AddHours(-1))
            .Create();
    }

    [Fact]
    public async Task ProcessesMultiplePrns()
    {
        var prns = new List<PrnUpdateStatus>
        {
            CreatePrnUpdateStatus("PRN-001"),
            CreatePrnUpdateStatus("PRN-002"),
            CreatePrnUpdateStatus("PRN-003"),
        };

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.UpdateRrepwPrns))
            .ReturnsAsync(DateTime.MinValue);

        _prnServiceMock
            .Setup(x =>
                x.GetUpdatedPrns(ItEx.IsCloseTo(DateTime.MinValue), ItEx.IsCloseTo(DateTime.UtcNow))
            )
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(prns) }
            );

        _rrepwServiceMock
            .Setup(x => x.UpdatePrn(It.IsAny<PrnUpdateStatus>()))
            .Callback(
                (PrnUpdateStatus prn) =>
                    prns.Find(p => p.PrnNumber == prn.PrnNumber).Should().BeEquivalentTo(prn)
            );
        await _function.Run(new TimerInfo());

        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(FunctionName.UpdateRrepwPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessesMultiplePrns_UseDates()
    {
        var prns = new List<PrnUpdateStatus>
        {
            CreatePrnUpdateStatus("PRN-001"),
            CreatePrnUpdateStatus("PRN-002"),
            CreatePrnUpdateStatus("PRN-003"),
        };
        var fromDate = DateTime.UtcNow.AddHours(-3);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.UpdateRrepwPrns))
            .ReturnsAsync(fromDate);
        _prnServiceMock
            .Setup(x => x.GetUpdatedPrns(ItEx.IsCloseTo(fromDate), ItEx.IsCloseTo(DateTime.UtcNow)))
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(prns) }
            );
        await _function.Run(new TimerInfo());
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(FunctionName.UpdateRrepwPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessesMultiplePrns_CatchesExceptionsFromGetUpdatedPrns()
    {
        var prns = new List<PrnUpdateStatus>
        {
            CreatePrnUpdateStatus("PRN-001"),
            CreatePrnUpdateStatus("PRN-002"),
            CreatePrnUpdateStatus("PRN-003"),
        };
        var fromDate = DateTime.UtcNow.AddHours(-3);
        var ex = new Exception("Test");
        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.UpdateRrepwPrns))
            .ReturnsAsync(fromDate);
        _prnServiceMock
            .Setup(x => x.GetUpdatedPrns(ItEx.IsCloseTo(fromDate), ItEx.IsCloseTo(DateTime.UtcNow)))
            .Throws(ex);

        await Assert.ThrowsAsync<Exception>(async () => await _function.Run(new TimerInfo()));
        _rrepwServiceMock.Verify(x => x.UpdatePrn(It.IsAny<PrnUpdateStatus>()), Times.Never);
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ProcessesMultiplePrns_CatchesExceptionsFromUpdatePrns()
    {
        var prns = new List<PrnUpdateStatus>
        {
            CreatePrnUpdateStatus("PRN-001"),
            CreatePrnUpdateStatus("PRN-002"),
            CreatePrnUpdateStatus("PRN-003"),
        };
        var fromDate = DateTime.UtcNow.AddHours(-3);
        var ex = new Exception("Test");
        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.UpdateRrepwPrns))
            .ReturnsAsync(fromDate);
        _prnServiceMock
            .Setup(x => x.GetUpdatedPrns(ItEx.IsCloseTo(fromDate), ItEx.IsCloseTo(DateTime.UtcNow)))
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(prns) }
            );
        _rrepwServiceMock.Setup(x => x.UpdatePrn(It.IsAny<PrnUpdateStatus>())).Throws(ex);

        await _function.Run(new TimerInfo());
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Once
        );
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    ex,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Exactly(3)
        );
    }

    [Fact]
    public async Task WhenNoBlobStorageValueExists_UsesDefaultStartDateFromConfiguration()
    {
        var prns = new List<PrnUpdateStatus>
        {
            CreatePrnUpdateStatus("PRN-001"),
            CreatePrnUpdateStatus("PRN-002"),
            CreatePrnUpdateStatus("PRN-003"),
        };

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
            .Setup(x => x.GetLastUpdate(FunctionName.UpdateRrepwPrns))
            .ReturnsAsync((DateTime?)null);

        _prnServiceMock
            .Setup(x =>
                x.GetUpdatedPrns(ItEx.IsCloseTo(new DateTime(2024, 01, 01)), It.IsAny<DateTime>())
            )
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(prns) }
            );
        _rrepwServiceMock
            .Setup(x => x.UpdatePrn(It.IsAny<PrnUpdateStatus>()))
            .Callback(
                (PrnUpdateStatus prn) =>
                    prns.Find(p => p.PrnNumber == prn.PrnNumber).Should().BeEquivalentTo(prn)
            );
        await _function.Run(new TimerInfo());

        // Verify GetLastUpdate was called
        _lastUpdateServiceMock.Verify(
            x => x.GetLastUpdate(FunctionName.UpdateRrepwPrns),
            Times.Once
        );

        // Verify last update was set
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(FunctionName.UpdateRrepwPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }
}
