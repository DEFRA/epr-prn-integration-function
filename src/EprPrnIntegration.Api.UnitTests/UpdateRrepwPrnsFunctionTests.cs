using AutoFixture;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Api.UnitTests.Helpers;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
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
        return _fixture.Build<PrnUpdateStatus>().With(p => p.PrnNumber, prnId).Create();
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
            .Setup(x => x.GetLastUpdate(UpdateRrepwPrnsFunction.FunctionId))
            .ReturnsAsync(DateTime.MinValue);

        _prnServiceMock
            .Setup(x =>
                x.GetUpdatedPrns(ItEx.IsCloseTo(DateTime.MinValue), ItEx.IsCloseTo(DateTime.UtcNow))
            )
            .ReturnsAsync(prns);

        await _function.Run(new TimerInfo());

        _rrepwServiceMock.Verify(x => x.UpdatePrns(prns), Times.Once);

        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(
                    UpdateRrepwPrnsFunction.FunctionId,
                    ItEx.IsCloseTo(DateTime.UtcNow)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessesMultiplePrns_Limit()
    {
        _config.Value.UpdateRrepwPrnsMaxRows = 2;
        var prns = new List<PrnUpdateStatus>
        {
            CreatePrnUpdateStatus("PRN-001"),
            CreatePrnUpdateStatus("PRN-002"),
            CreatePrnUpdateStatus("PRN-003"),
        };
        prns[0].StatusDate = DateTime.UtcNow.AddHours(-3);
        prns[1].StatusDate = DateTime.UtcNow.AddHours(-2);
        prns[2].StatusDate = DateTime.UtcNow.AddHours(-1);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(UpdateRrepwPrnsFunction.FunctionId))
            .ReturnsAsync(DateTime.MinValue);

        _prnServiceMock
            .Setup(x =>
                x.GetUpdatedPrns(ItEx.IsCloseTo(DateTime.MinValue), ItEx.IsCloseTo(DateTime.UtcNow))
            )
            .ReturnsAsync(prns);

        await _function.Run(new TimerInfo());

        _rrepwServiceMock.Verify(
            x =>
                x.UpdatePrns(It.Is<List<PrnUpdateStatus>>(l => l[0] == prns[0] && l[1] == prns[1])),
            Times.Once
        );

        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(
                    UpdateRrepwPrnsFunction.FunctionId,
                    ItEx.IsCloseTo(DateTime.UtcNow)
                ),
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
            .Setup(x => x.GetLastUpdate(UpdateRrepwPrnsFunction.FunctionId))
            .ReturnsAsync(fromDate);

        await _function.Run(new TimerInfo());

        _prnServiceMock
            .Setup(x => x.GetUpdatedPrns(ItEx.IsCloseTo(fromDate), ItEx.IsCloseTo(DateTime.UtcNow)))
            .ReturnsAsync(prns);
    }

    [Fact]
    public async Task ProcessesMultiplePrns_CatchesExceptions()
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
            .Setup(x => x.GetLastUpdate(UpdateRrepwPrnsFunction.FunctionId))
            .ReturnsAsync(fromDate);
        _prnServiceMock
            .Setup(x => x.GetUpdatedPrns(ItEx.IsCloseTo(fromDate), ItEx.IsCloseTo(DateTime.UtcNow)))
            .Throws(ex);

        await _function.Run(new TimerInfo());
        _rrepwServiceMock.Verify(x => x.UpdatePrns(It.IsAny<List<PrnUpdateStatus>>()), Times.Never);
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    ex,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
