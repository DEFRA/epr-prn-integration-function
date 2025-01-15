using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text;

namespace EprPrnIntegration.Common.UnitTests.Helpers;

public class UtilitiesTests
{
    private readonly Mock<IServiceBusProvider> _serviceBusProviderMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Utilities _utilities;
    private readonly Mock<ITelemetryChannel> _mockTelemetryChannel;

    public UtilitiesTests()
    {
        _serviceBusProviderMock = new Mock<IServiceBusProvider>();
        _configurationMock = new Mock<IConfiguration>();
        _mockTelemetryChannel = new Mock<ITelemetryChannel>();

        TelemetryConfiguration configuration = new()
        {
            TelemetryChannel = _mockTelemetryChannel.Object
        };

        configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
        TelemetryClient telemetryClient = new(configuration);

        _utilities = new Utilities(_serviceBusProviderMock.Object, _configurationMock.Object, telemetryClient);
    }

    [Fact]
    public async Task GetDeltaSyncExecution_ShouldReturnDefaultWhenNoMessageInQueue()
    {
        // Arrange
        var syncType = NpwdDeltaSyncType.UpdatedProducers;
        var defaultLastRunDate = "2024-01-01";
        _configurationMock.Setup(c => c["DefaultLastRunDate"]).Returns(defaultLastRunDate);

        _serviceBusProviderMock.Setup(provider => provider.GetDeltaSyncExecutionFromQueue(syncType))
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

        _serviceBusProviderMock.Setup(provider => provider.GetDeltaSyncExecutionFromQueue(syncType))
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

    [Fact]
    public void AddCustomEvent_CallsTelementryClient()
    {
        _utilities.AddCustomEvent("Event", new Dictionary<string, string>()
        {
            {"test1", "test1" }
        });

        _mockTelemetryChannel.Verify(t => t.Send(It.IsAny<ITelemetry>()), Times.Once);
    }

    [Fact]
    public void CreateCsvContent_GeneratesCorrectCsv_ForValidData()
    {
        // Arrange
        var data = new Dictionary<string, List<string>>
        {
            { "Column1", ["Value1", "Value2"] },
            { "Column2", ["ValueA", "ValueB"] }
        };

        var expectedCsv = "Column1,Column2\nValue1,ValueA\nValue2,ValueB\n";

        // Act
        var result = _utilities.CreateCsvContent(data);

        // Normalize line endings for comparison
        var normalizedResult = result.Replace("\r\n", "\n");
        var normalizedExpected = expectedCsv.Replace("\r\n", "\n");

        // Assert
        Assert.Equal(normalizedExpected, normalizedResult);
    }

    [Fact]
    public void CreateCsvContent_HandlesEmptyValuesCorrectly()
    {
        // Arrange
        var data = new Dictionary<string, List<string>>
        {
            { "Column1", ["Value1", "Value2", "Value3"] },
            { "Column2", ["ValueA"] }
        };

        var expectedCsv = "Column1,Column2\nValue1,ValueA\nValue2,\nValue3,\n";

        // Act
        var result = _utilities.CreateCsvContent(data);

        // Normalize line endings for comparison
        var normalizedResult = result.Replace("\r\n", "\n");
        var normalizedExpected = expectedCsv.Replace("\r\n", "\n");

        // Assert
        Assert.Equal(normalizedExpected, normalizedResult);
    }

    [Fact]
    public void CreateCsvContent_ReturnsHeaderOnly_WhenDataIsEmpty()
    {
        // Arrange
        var data = new Dictionary<string, List<string>>
        {
            { "Column1", [] },
            { "Column2", [] }
        };

        var expectedCsv = "Column1,Column2\n";

        // Act
        var result = _utilities.CreateCsvContent(data);

        // Normalize line endings for comparison
        var normalizedResult = result.Replace("\r\n", "\n");
        var normalizedExpected = expectedCsv.Replace("\r\n", "\n");

        // Assert
        Assert.Equal(normalizedExpected, normalizedResult);
    }

    [Fact]
    public void CreateCsvContent_HandlesSpecialCharactersCorrectly()
    {
        // Arrange
        var data = new Dictionary<string, List<string>>
        {
            { "Column1", ["Value1", "Value, with, commas"] },
            { "Column2", ["ValueA", "\"QuotedValue\""] }
        };

        var expectedCsv = "Column1,Column2\nValue1,ValueA\n\"Value, with, commas\",\"\"\"QuotedValue\"\"\"\n";

        // Act
        var result = _utilities.CreateCsvContent(data);

        // Normalize line endings for comparison
        var normalizedResult = result.Replace("\r\n", "\n");
        var normalizedExpected = expectedCsv.Replace("\r\n", "\n");

        // Assert
        Assert.Equal(normalizedExpected, normalizedResult);
    }

}