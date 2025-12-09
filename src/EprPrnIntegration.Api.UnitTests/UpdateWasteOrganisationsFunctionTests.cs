using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.Azure.Functions.Worker;
using System.Net;

namespace EprPrnIntegration.Api.UnitTests;

public class UpdateWasteOrganisationsFunctionTests
{
    private readonly Mock<ILogger<UpdateWasteOrganisationsFunction>> _loggerMock = new();
    private readonly Mock<ILastUpdateService> _lastUpdateServiceMock = new();
    private readonly Mock<IWasteOrganisationsService> _wasteOrganisationsService = new();
    private readonly Mock<ICommonDataService> _commonDataService = new();

    private readonly UpdateWasteOrganisationsFunction _function;

    public UpdateWasteOrganisationsFunctionTests()
    {
        _function = new(_lastUpdateServiceMock.Object, _loggerMock.Object, _commonDataService.Object, _wasteOrganisationsService.Object);
    }

    [Fact]
    public async Task ProcessesMultipleProducers()
    {
        var producers = new List<UpdatedProducersResponseV2>
        {
            CreateProducer("producer-1"),
            CreateProducer("producer-2", "CS"),
            CreateProducer("producer-3", status: "deleted")
        };

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _commonDataService.Setup(x => x.GetUpdatedProducersV2(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(producers);

        await _function.Run(new TimerInfo());

        _wasteOrganisationsService.Verify(
            x => x.UpdateOrganisation("producer-1", It.IsAny<Common.Models.WasteOrganisationsApi.WasteOrganisationsApiUpdateRequest>()),
            Times.Once);
        _wasteOrganisationsService.Verify(
            x => x.UpdateOrganisation("producer-2", It.IsAny<Common.Models.WasteOrganisationsApi.WasteOrganisationsApiUpdateRequest>()),
            Times.Once);
        _wasteOrganisationsService.Verify(
            x => x.UpdateOrganisation("producer-3", It.IsAny<Common.Models.WasteOrganisationsApi.WasteOrganisationsApiUpdateRequest>()),
            Times.Once);
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task WhenCommonDataServiceThrows_DoesNotSetLastUpdateTime()
    {
        var expectedException = new HttpRequestException("Service unavailable");

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _commonDataService.Setup(x =>
                x.GetUpdatedProducersV2(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        await _function.Run(new TimerInfo());

        // Verify last update was NOT set
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task WhenCommonDataReturnsZeroItems_DoesNotSetLastUpdateTime()
    {
        var emptyProducersList = new List<UpdatedProducersResponseV2>();

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _commonDataService.Setup(x =>
                x.GetUpdatedProducersV2(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyProducersList);

        await _function.Run(new TimerInfo());

        // Verify last update was NOT set
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task WhenOneWasteServiceRequestFails_ContinuesProcessingAndUpdatesLastUpdatedTime()
    {
        var producers = new List<UpdatedProducersResponseV2>
        {
            CreateProducer("producer-1"),
            CreateProducer("producer-2-fails", "CS"),
            CreateProducer("producer-3", status: "deleted")
        };

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _commonDataService.Setup(x => x.GetUpdatedProducersV2(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(producers);

        _wasteOrganisationsService
            .Setup(x => x.UpdateOrganisation("producer-2-fails", It.IsAny<Common.Models.WasteOrganisationsApi.WasteOrganisationsApiUpdateRequest>()))
            .ThrowsAsync(new HttpRequestException("Missing credentials", null, HttpStatusCode.Unauthorized));

        await _function.Run(new TimerInfo());

        // Verify all three producers were attempted
        _wasteOrganisationsService.Verify(
            x => x.UpdateOrganisation("producer-1", It.IsAny<Common.Models.WasteOrganisationsApi.WasteOrganisationsApiUpdateRequest>()),
            Times.Once);
        _wasteOrganisationsService.Verify(
            x => x.UpdateOrganisation("producer-2-fails", It.IsAny<Common.Models.WasteOrganisationsApi.WasteOrganisationsApiUpdateRequest>()),
            Times.Once);
        _wasteOrganisationsService.Verify(
            x => x.UpdateOrganisation("producer-3", It.IsAny<Common.Models.WasteOrganisationsApi.WasteOrganisationsApiUpdateRequest>()),
            Times.Once);

        // Verify last update WAS still set despite one failure
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task WhenTransientErrorOccurs_ForWasteOrganisationApi_RethrowsExceptionAndDoesNotUpdateLastUpdatedTime(HttpStatusCode statusCode)
    {
        var producers = new List<UpdatedProducersResponseV2>
        {
            CreateProducer("producer-1"),
            CreateProducer("producer-2-transient", "CS")
        };

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _commonDataService.Setup(x => x.GetUpdatedProducersV2(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(producers);

        _wasteOrganisationsService
            .Setup(x => x.UpdateOrganisation("producer-2-transient", It.IsAny<Common.Models.WasteOrganisationsApi.WasteOrganisationsApiUpdateRequest>()))
            .ThrowsAsync(new HttpRequestException($"Error: {statusCode}", null, statusCode));

        // Act & Assert - expect the exception to be rethrown
        await Assert.ThrowsAsync<HttpRequestException>(() => _function.Run(new TimerInfo()));

        // Verify last update was NOT set when transient error occurs
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }
    
    private static UpdatedProducersResponseV2 CreateProducer(string peprid, string type = "DP", string status = "registered")
    {
        return new()
        {
            PEPRID = peprid,
            OrganisationName = $"Producer {peprid}",
            Status = status,
            OrganisationType = type,
            RegistrationYear = "2025"
        };
    }
}