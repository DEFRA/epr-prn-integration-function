using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using FluentAssertions;
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
    public async Task CreateErrorEventsCsvStreamAsync_ShouldReturnValidCsvStream()
    {
        // Arrange
        var errorEvents = new List<ErrorEvent>
        {
            new ErrorEvent
            {
                PrnNumber = "12345",
                IncomingStatus = "Active",
                Date = "01/07/2025",
                OrganisationName = "Example Org",
                ErrorComments = "Sample error message"
            },
            new ErrorEvent
            {
                PrnNumber = "67890",
                IncomingStatus = "Inactive",
                Date = "02/07/2025",
                OrganisationName = "Another Org",
                ErrorComments = "Another sample message"
            }
        };

        // Act
        var csvStream = await _utilities.CreateErrorEventsCsvStreamAsync(errorEvents);

        // Assert
        csvStream.Position = 0; // Ensure the stream is at the beginning for reading
        using var reader = new StreamReader(csvStream);
        var csvContent = await reader.ReadToEndAsync();

        var expectedCsv = new StringBuilder()
            .AppendLine("PRN Number,Incoming Status,Date,Organisation Name,Error Comments")
            .AppendLine("12345,Active,01/07/2025,Example Org,Sample error message")
            .AppendLine("67890,Inactive,02/07/2025,Another Org,Another sample message")
            .ToString();

        Assert.Equal(expectedCsv, csvContent);
    }

    [Fact]
    public async Task CreateErrorEventsCsvStreamAsync_ShouldHandleEmptyList()
    {
        // Arrange
        var errorEvents = new List<ErrorEvent>();

        // Act
        var csvStream = await _utilities.CreateErrorEventsCsvStreamAsync(errorEvents);

        // Assert
        csvStream.Position = 0;
        using var reader = new StreamReader(csvStream);
        var csvContent = await reader.ReadToEndAsync();

        Assert.Empty(csvContent); // The CSV should be empty for an empty list
    }

    [Fact]
    public async Task CreateErrorEventsCsvStreamAsync_ShouldHandleNullList()
    {
        // Arrange
        List<ErrorEvent> errorEvents = null;

        // Act
        var csvStream = await _utilities.CreateErrorEventsCsvStreamAsync(errorEvents);

        // Assert
        csvStream.Position = 0;
        using var reader = new StreamReader(csvStream);
        var csvContent = await reader.ReadToEndAsync();

        Assert.Empty(csvContent); // The CSV should be empty for a null list
    }

    [Theory]
    [InlineData(null)]
    [InlineData("11223344556677889900")]
    [InlineData("Not an integer")]
    [InlineData("-1")]
    public void OffsetDateTimeWithLag_WhenMisconfigured_ShouldReturnDefault(string configSeconds)
    {
        // Arrange
        DateTime expectedDateTime = DateTime.UtcNow;
        DateTime pollingDateTime = expectedDateTime.AddSeconds(60);

        // Act
        DateTime actualDateTime = _utilities.OffsetDateTimeWithLag(pollingDateTime, configSeconds);

        // Assert
        expectedDateTime.Should().Be(actualDateTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void OffsetDateTimeWithLag_WhenConfigured_ShouldReturnAdjsutedDateTime(int seconds)
    {
        // Arrange
        DateTime expectedDateTime = DateTime.UtcNow;
        DateTime pollingDateTime = expectedDateTime.AddSeconds(seconds);

        // Act
        DateTime actualDateTime = _utilities.OffsetDateTimeWithLag(pollingDateTime, seconds.ToString());

        // Assert
        expectedDateTime.Should().Be(actualDateTime);
    }

}