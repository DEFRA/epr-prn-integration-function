using AutoFixture;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;

namespace EprPrnIntegration.Api.UnitTests;

public class UpdateProducersFunctionTests
{
    private readonly Mock<ICommonDataService> _commonDataServiceMock = new();
    private readonly Mock<INpwdClient> _npwdClientMock = new();
    private readonly Mock<ILogger<UpdateProducersFunction>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IUtilities> _utilitiesMock = new();
    private readonly Mock<IOptions<FeatureManagementConfiguration>> _mockFeatureConfig = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();

    private readonly UpdateProducersFunction function;

    public UpdateProducersFunctionTests()
    {
        // Turn the feature flag on
        var config = new FeatureManagementConfiguration
        {
            RunIntegration = false,
            RunUpdateProducers = true
        };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        function = new UpdateProducersFunction(_commonDataServiceMock.Object, _npwdClientMock.Object,
            _loggerMock.Object, _configurationMock.Object, _utilitiesMock.Object, _mockFeatureConfig.Object, _emailServiceMock.Object);
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

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await function.Run(null!);

        // Assert
        _commonDataServiceMock.Verify(
            service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        _npwdClientMock.Verify(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers),
            Times.Once);
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Producers list successfully updated")),
            null,
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Run_NoUpdatedProducers_LogsWarning()
    {
        // Arrange
        _configurationMock.Setup(c => c["DefaultLastRunDate"]).Returns("2024-01-01");

        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedProducersResponse>());

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await function.Run(null!);

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
        _configurationMock.Setup(c => c["DefaultLastRunDate"]).Returns("2024-01-01");

        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Fetch error"));

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await Assert.ThrowsAsync<Exception>(() => function.Run(null!));

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Failed to retrieve data")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Run_FailedToUpdateProducers_LogsRawResponseBody()
    {
        // Arrange
        _configurationMock.Setup(c => c["DefaultLastRunDate"]).Returns("2024-01-01");
        var updatedProducers = new List<UpdatedProducersResponse> { new UpdatedProducersResponse() };

        var responseBody =
            "[{\"error\":{\"code\":\"400 BadRequest\",\"message\":\"The EPRCode field is required.\",\"details\":{\"targetField\":\"EPRCode\",\"targetRecordId\":\"2498a75c-9659-4e7f-b86f-eada60d0e72c\"}}}]";

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent(responseBody)
        };

        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers))
            .ReturnsAsync(responseMessage);

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await function.Run(null!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                $"{v}".ToString()
                    .Contains(
                        $"Failed to update producer lists. error code {HttpStatusCode.BadRequest} and raw response body: {responseBody}")),
            null,
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Run_Ends_When_Feature_Flag_Is_False()
    {
        // Arrange
        var config = new FeatureManagementConfiguration
        {
            RunIntegration = false
        };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        // Act
        await function.Run(null!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
                  It.IsAny<LogLevel>(),
                  It.IsAny<EventId>(),
                  It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("UpdateProducersList function is disabled by feature flag")),

                  It.IsAny<Exception>(),
                  It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
              Times.Once());
    }

    [Fact]
    public async Task Run_SendsDeltaSyncExecutionToQueue()
    {
        // Arrange
        var updatedProducers = _fixture.CreateMany<UpdatedProducersResponse>().ToList();

        _commonDataServiceMock
            .Setup(service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        // Mock DeltaSyncExecution
        var deltaSyncExecution = new DeltaSyncExecution
        {
            SyncType = NpwdDeltaSyncType.UpdatedProducers,
            LastSyncDateTime = DateTime.UtcNow.AddHours(-1)
        };

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(deltaSyncExecution);

        // Act
        await function.Run(null!);

        // Assert
        _utilitiesMock.Verify(
            provider => provider.SetDeltaSyncExecution(
                It.Is<DeltaSyncExecution>(d => d.SyncType == NpwdDeltaSyncType.UpdatedProducers), It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_NoMessageInQueue_UsesDefaultFromConfig()
    {
        // Arrange
        var defaultDatetime = "2024-01-01";
        var updatedProducers = _fixture.CreateMany<UpdatedProducersResponse>().ToList();

        var defaultDeltaSync = new DeltaSyncExecution
        {
            LastSyncDateTime = DateTime.Parse(defaultDatetime),
            SyncType = NpwdDeltaSyncType.UpdatedProducers
        };

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(defaultDeltaSync);

        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        // Act
        await function.Run(null!);

        // Assert: Verify that DeltaSyncExecution is created using the default date from config
        _commonDataServiceMock.Verify(service =>
            service.GetUpdatedProducers(DateTime.Parse(defaultDatetime), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_AddCustomEventForUpdateProducers()
    {
        // Arrange
        var updatedProducers = _fixture.CreateMany<UpdatedProducersResponse>().ToList();
        _utilitiesMock.Setup(u => u.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers)).ReturnsAsync(_fixture.Create<DeltaSyncExecution>());
        
        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([updatedProducers[0]]);

        IEnumerable<Producer> mappedProducers = null;

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers))
            .ReturnsAsync((ProducerDelta delta, string path) => 
            {
                mappedProducers = delta.Value;
                return new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK };
            });

        // Act
        await function.Run(new());

        Assert.NotNull(mappedProducers);
        var npwdSentMappedProducer = mappedProducers.FirstOrDefault();
        Assert.NotNull(npwdSentMappedProducer);

        _utilitiesMock.Verify(u => u.AddCustomEvent(It.Is<string>(s => s == CustomEvents.UpdateProducer),
            It.Is<Dictionary<string, string>>(
                data => data[CustomEventFields.OrganisationName] == npwdSentMappedProducer.ProducerName
                && data[CustomEventFields.OrganisationId] == npwdSentMappedProducer.EPRCode
                && data[CustomEventFields.OrganisationAddress] == MapProducerAddress(npwdSentMappedProducer)
                && data[CustomEventFields.OrganisationType] == npwdSentMappedProducer.EntityTypeCode
                && data[CustomEventFields.OrganisationStatus] == npwdSentMappedProducer.StatusCode
                && data[CustomEventFields.OrganisationEprId] == npwdSentMappedProducer.EPRId
                && data[CustomEventFields.OrganisationRegNo] == npwdSentMappedProducer.CompanyRegNo)
            ), Times.Once);
    }

    [Fact]
    public async Task Run_ValidStartHour_FetchAndUpdatesProducers_HandlesNpwdClientErrorsCorrectly()
    {
        // Arrange
        var updatedProducers = new List<UpdatedProducersResponse> { new() };

        _commonDataServiceMock
            .Setup(service =>
                service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers))
            .Throws<Exception>();

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });        

        // Act
        await function.Run(null!);

        // Assert
        _commonDataServiceMock.Verify(
            service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _npwdClientMock.Verify(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers),
            Times.Once);

        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("An error was encountered on attempting to call NPWD API ")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task FetchAndUpdatesProducers_Sendemail_When_Error_Occurs(HttpStatusCode statusCode)
    {
        // Arrange      
        var updatedProducers = new List<UpdatedProducersResponse> { new() };

        _commonDataServiceMock
             .Setup(service =>
                 service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(updatedProducers);

        _npwdClientMock.Setup(x => x.Patch(It.IsAny<ProducerDelta?>(), It.IsAny<string>()))
           .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("Server Error") });

        _utilitiesMock
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatedProducers))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatedProducers,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await function.Run(null!);

        // Assert
        _commonDataServiceMock.Verify(
            service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _npwdClientMock.Verify(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.Producers),
            Times.Once);

       _emailServiceMock.Verify(x => x.SendErrorEmailToNpwd(It.IsAny<string>()), Times.Once);
    }

    private static string MapProducerAddress(Producer producer)
    {
        if (producer == null)
            return string.Empty;

        var addressFields = new[]
        {
                producer.AddressLine1,
                producer.AddressLine2,
                producer.Town,
                producer.County,
                producer.Postcode,
            };

        return string.Join(", ", addressFields.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}