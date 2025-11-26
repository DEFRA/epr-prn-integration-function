using AutoFixture;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Models.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using EprPrnIntegration.Common.Models.Rrepw;
using Xunit;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;

namespace EprPrnIntegration.Api.UnitTests;

public class UpdateRrepwProducersFunctionTests
{
    private readonly Mock<ICommonDataService> _commonDataServiceMock = new();
    private readonly Mock<IRrepwClient> _rrepwClientMock = new();
    private readonly Mock<ILogger<UpdateRrepwProducersFunction>> _loggerMock = new();
    private readonly Mock<IUtilities> _utilitiesMock = new();
    private readonly Fixture _fixture = new();

    private readonly UpdateRrepwProducersFunction _function;

    public UpdateRrepwProducersFunctionTests()
    {
        _function = new UpdateRrepwProducersFunction(_commonDataServiceMock.Object, _rrepwClientMock.Object,
        _loggerMock.Object, _utilitiesMock.Object);
    }

    [Fact]
    public async Task Run_ValidStartHour_FetchesAndUpdatesProducers()
    {
        // Arrange
        var updatedProducers = _fixture.CreateMany<UpdatedProducersResponse>().ToList();

        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _rrepwClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerUpdateRequest>()))
            .Returns(Task.CompletedTask);

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedRrepwProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedRrepwProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await _function.Run(null!);

        // Assert
        _commonDataServiceMock.Verify(
            service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        _rrepwClientMock.Verify(client => client.Patch(It.IsAny<ProducerUpdateRequest>()),Times.Exactly(updatedProducers.Count));
    }

    [Fact]
    public async Task Run_NoUpdatedProducers_LogsWarning()
    {
        // Arrange
        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedProducersResponse>());

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedRrepwProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedRrepwProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await _function.Run(null!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("No updated producers")),
            null,
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Run_FailedToFetchData_LogsError()
    {
        // Arrange
        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Fetch error"));

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedRrepwProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedRrepwProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await Assert.ThrowsAsync<Exception>(() => _function.Run(null!));

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Failed to retrieve data")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Run_SendsDeltaSyncExecutionToQueue()
    {
        // Arrange
        var updatedProducers = _fixture.CreateMany<UpdatedProducersResponse>().ToList();

        _commonDataServiceMock
            .Setup(service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        // Mock DeltaSyncExecution
        var deltaSyncExecution = new DeltaSyncExecution
        {
            SyncType = NpwdDeltaSyncType.UpdatedRrepwProducers,
            LastSyncDateTime = DateTime.UtcNow.AddHours(-1)
        };

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedRrepwProducers))
            .ReturnsAsync(deltaSyncExecution);

        // Act
        await _function.Run(null!);

        // Assert
        _utilitiesMock.Verify(
            provider => provider.SetDeltaSyncExecution(
                It.Is<DeltaSyncExecution>(d => d.SyncType == NpwdDeltaSyncType.UpdatedRrepwProducers), It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_AlwaysSetsToDate_ToNewestUpdatedDateTime()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var updatedProducers = _fixture.CreateMany<UpdatedProducersResponse>().ToList();
        updatedProducers[0].UpdatedDateTime = now.AddMinutes(-10);
        updatedProducers[1].UpdatedDateTime = now.AddMinutes(-5);
        updatedProducers[2].UpdatedDateTime = now;

        _commonDataServiceMock
            .Setup(service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        var deltaSyncExecution = new DeltaSyncExecution
        {
            SyncType = NpwdDeltaSyncType.UpdatedRrepwProducers,
            LastSyncDateTime = now.AddHours(-1)
        };

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedRrepwProducers))
            .ReturnsAsync(deltaSyncExecution);

        _rrepwClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerUpdateRequest>()))
            .Returns(Task.CompletedTask);
        
        DateTime? capturedToDate = null;
        _utilitiesMock
            .Setup(u => u.SetDeltaSyncExecution(It.IsAny<DeltaSyncExecution>(), It.IsAny<DateTime>()))
            .Callback<DeltaSyncExecution, DateTime>((_, toDate) => capturedToDate = toDate)
            .Returns(Task.CompletedTask);

        // Act
        await _function.Run(null!);

        // Assert
        Assert.NotNull(capturedToDate);
        Assert.Equal(now, capturedToDate.Value, TimeSpan.FromSeconds(1)); // within 1 second is acceptable due to truncation
    }
}