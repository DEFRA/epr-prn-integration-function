using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests;

public class UpdateProducersFunctionTests
{
    private readonly Mock<IOrganisationService> _organisationServiceMock = new();
    private readonly Mock<INpwdClient> _npwdClientMock = new();
    private readonly Mock<ILogger<UpdateProducersFunction>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IServiceBusProvider> _serviceBusProviderMock = new();

    // Test: Check if the function handles valid StartHour properly
    [Fact]
    public async Task Run_ValidStartHour_FetchesAndUpdatesProducers()
    {
        // Arrange
        var startHour = 18;
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns(startHour.ToString());
        var updatedProducers = new List<UpdatedProducersResponseModel> { new UpdatedProducersResponseModel() };

        _organisationServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.UpdateProducers))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        _serviceBusProviderMock
            .Setup(provider => provider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object,
            _loggerMock.Object, _configurationMock.Object, _serviceBusProviderMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _organisationServiceMock.Verify(
            service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        _npwdClientMock.Verify(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.UpdateProducers),
            Times.Once);
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Producers list successfully updated")),
            null,
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    // Test: Ensure that when no updated producers are found, a warning is logged
    [Fact]
    public async Task Run_NoUpdatedProducers_LogsWarning()
    {
        // Arrange
        var startHour = 18;
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns(startHour.ToString());

        _organisationServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedProducersResponseModel>());

        _serviceBusProviderMock
            .Setup(provider => provider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object,
            _loggerMock.Object, _configurationMock.Object, _serviceBusProviderMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No updated producers")),
            null,
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    // Test: Handle when fetching updated producers fails, logs an error
    [Fact]
    public async Task Run_FailedToFetchData_LogsError()
    {
        // Arrange
        var startHour = 18;
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns(startHour.ToString());

        _organisationServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Fetch error"));

        _serviceBusProviderMock
            .Setup(provider => provider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object,
            _loggerMock.Object, _configurationMock.Object, _serviceBusProviderMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve data")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    // Test: Handle API response failure, logs raw response body
    [Fact]
    public async Task Run_FailedToUpdateProducers_LogsRawResponseBody()
    {
        // Arrange
        var startHour = 18;
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns(startHour.ToString());
        var updatedProducers = new List<UpdatedProducersResponseModel> { new UpdatedProducersResponseModel() };

        var responseBody =
            "[{\"error\":{\"code\":\"400 BadRequest\",\"message\":\"The EPRCode field is required.\",\"details\":{\"targetField\":\"EPRCode\",\"targetRecordId\":\"2498a75c-9659-4e7f-b86f-eada60d0e72c\"}}}]";

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.BadRequest,
            Content = new StringContent(responseBody)
        };

        _organisationServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.UpdateProducers))
            .ReturnsAsync(responseMessage);

        _serviceBusProviderMock
            .Setup(provider => provider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object,
            _loggerMock.Object, _configurationMock.Object, _serviceBusProviderMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString().Contains($"Failed to parse error response body. Raw Response Body: {responseBody}")),
            null,
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Run_SendsDeltaSyncExecutionToQueue()
    {
        // Arrange
        var startHour = 18;
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns(startHour.ToString());
        var updatedProducers = new List<UpdatedProducersResponseModel> { new UpdatedProducersResponseModel() };

        _organisationServiceMock
            .Setup(service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.UpdateProducers))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        // Mock DeltaSyncExecution
        var deltaSyncExecution = new DeltaSyncExecution
        {
            SyncType = NpwdDeltaSyncType.UpdatedProducers,
            LastSyncDateTime = DateTime.UtcNow.AddHours(-1)
        };

        _serviceBusProviderMock
            .Setup(provider => provider.ReceiveDeltaSyncExecutionFromQueue(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(deltaSyncExecution);

        _serviceBusProviderMock
            .Setup(provider => provider.SendDeltaSyncExecutionToQueue(It.IsAny<DeltaSyncExecution>()))
            .Returns(Task.CompletedTask); // Mock the Send method to complete successfully.

        var function = new UpdateProducersFunction(
            _organisationServiceMock.Object,
            _npwdClientMock.Object,
            _loggerMock.Object,
            _configurationMock.Object,
            _serviceBusProviderMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _serviceBusProviderMock.Verify(provider => provider.SendDeltaSyncExecutionToQueue(It.Is<DeltaSyncExecution>(d => d.SyncType == NpwdDeltaSyncType.UpdatedProducers)), Times.Once);
    }
}