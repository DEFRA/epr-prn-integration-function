using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Helpers;

public class UtilitiesTests
{
    private readonly Mock<IServiceBusProvider> _serviceBusProviderMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Utilities _utilities;

    public UtilitiesTests()
    {
        _serviceBusProviderMock = new Mock<IServiceBusProvider>();
        _configurationMock = new Mock<IConfiguration>();
        _utilities = new Utilities(_serviceBusProviderMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task GetDeltaSyncExecution_ShouldReturnDefaultWhenNoMessageInQueue()
    {
        // Arrange
        var syncType = NpwdDeltaSyncType.UpdatedProducers;
        var defaultLastRunDate = "2024-01-01";
        _configurationMock.Setup(c => c["DefaultLastRunDate"]).Returns(defaultLastRunDate);

        _serviceBusProviderMock.Setup(provider => provider.ReceiveDeltaSyncExecutionFromQueue(syncType))
            .ReturnsAsync((DeltaSyncExecution)null);

        // Act
        var result = await _utilities.GetDeltaSyncExecution(syncType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(syncType, result.SyncType);
        Assert.Equal(DateTime.Parse(defaultLastRunDate), result.LastSyncDateTime);
    }

    [Fact]
    public async Task GetDeltaSyncExecution_ShouldReturnMessageWhenExistsInQueue()
    {
        // Arrange
        var syncType = NpwdDeltaSyncType.UpdatedProducers;
        var expectedExecution = new DeltaSyncExecution
        {
            SyncType = syncType,
            LastSyncDateTime = DateTime.UtcNow.AddHours(-1)
        };

        _serviceBusProviderMock.Setup(provider => provider.ReceiveDeltaSyncExecutionFromQueue(syncType))
            .ReturnsAsync(expectedExecution);

        // Act
        var result = await _utilities.GetDeltaSyncExecution(syncType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(syncType, result.SyncType);
        Assert.Equal(expectedExecution.LastSyncDateTime, result.LastSyncDateTime);
    }

    [Fact]
    public async Task SetDeltaSyncExecution_ShouldSendMessageToQueue()
    {
        // Arrange
        var syncExecution = new DeltaSyncExecution
        {
            SyncType = NpwdDeltaSyncType.UpdatedProducers,
            LastSyncDateTime = DateTime.UtcNow.AddHours(-1)
        };
        var latestRun = DateTime.UtcNow;

        _serviceBusProviderMock.Setup(provider => provider.SendDeltaSyncExecutionToQueue(syncExecution))
            .Returns(Task.CompletedTask);

        // Act
        await _utilities.SetDeltaSyncExecution(syncExecution, latestRun);

        // Assert
        Assert.Equal(latestRun, syncExecution.LastSyncDateTime);
        _serviceBusProviderMock.Verify(provider => provider.SendDeltaSyncExecutionToQueue(syncExecution), Times.Once);
    }
}