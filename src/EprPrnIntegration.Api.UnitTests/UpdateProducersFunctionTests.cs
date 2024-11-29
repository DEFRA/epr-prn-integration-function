using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
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

    public UpdateProducersFunctionTests()
    {
        // Turn the feature flag on
        _configurationMock.Setup(c => c["RunIntegration"]).Returns("True");
    }

    [Fact]
    public async Task Run_InvalidStartHour_UsesDefaultValue()
    {
        // Arrange
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns("invalid");
        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object, _loggerMock.Object, _configurationMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid StartHour configuration value")),
            null,
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Run_ValidStartHour_FetchesAndUpdatesProducers()
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

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object, _loggerMock.Object, _configurationMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _organisationServiceMock.Verify(service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _npwdClientMock.Verify(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.UpdateProducers), Times.Once);
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Producers list successfully updated")),
            null,
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Run_NoUpdatedProducers_LogsWarning()
    {
        // Arrange
        var startHour = 18;
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns(startHour.ToString());

        _organisationServiceMock
            .Setup(service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedProducersResponseModel>());

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object, _loggerMock.Object, _configurationMock.Object);

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

    [Fact]
    public async Task Run_FailedToFetchData_LogsError()
    {
        // Arrange
        var startHour = 18;
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns(startHour.ToString());

        _organisationServiceMock
            .Setup(service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Fetch error"));

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object, _loggerMock.Object, _configurationMock.Object);

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

    [Fact]
    public async Task Run_FailedToUpdateProducers_LogsRawResponseBody()
    {
        // Arrange
        var startHour = 18;
        _configurationMock.Setup(c => c["UpdateProducersStartHour"]).Returns(startHour.ToString());
        var updatedProducers = new List<UpdatedProducersResponseModel> { new UpdatedProducersResponseModel() };

        var responseBody = "[{\"error\":{\"code\":\"400 BadRequest\",\"message\":\"The EPRCode field is required.\",\"details\":{\"targetField\":\"EPRCode\",\"targetRecordId\":\"2498a75c-9659-4e7f-b86f-eada60d0e72c\"}}}]";

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.BadRequest,
            Content = new StringContent(responseBody)
        };

        _organisationServiceMock
            .Setup(service => service.GetUpdatedProducers(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProducers);

        _npwdClientMock
            .Setup(client => client.Patch(It.IsAny<ProducerDelta>(), NpwdApiPath.UpdateProducers))
            .ReturnsAsync(responseMessage);

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object, _loggerMock.Object, _configurationMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Failed to parse error response body. Raw Response Body: {responseBody}")),
            null,
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("False")]
    public async Task Run_Ends_When_Feature_Flag_Is_Flase_Or_Not_Set(string featureFlag)
    {
        // Arrange
        _configurationMock.Setup(c => c["RunIntegration"]).Returns(featureFlag);

        var function = new UpdateProducersFunction(_organisationServiceMock.Object, _npwdClientMock.Object, _loggerMock.Object, _configurationMock.Object);

        // Act
        await function.Run(null);

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.Contains("UpdateProducersList function is turned off"))));
        _loggerMock.Verify(logger => logger.Log(
                  It.IsAny<LogLevel>(),
                  It.IsAny<EventId>(),
                  It.IsAny<It.IsAnyType>(),
                  It.IsAny<Exception>(),
                  It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
              Times.Once());
    }
}