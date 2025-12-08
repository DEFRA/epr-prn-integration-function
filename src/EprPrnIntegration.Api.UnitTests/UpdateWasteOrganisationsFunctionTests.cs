using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.CommonService.Interfaces;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.Azure.Functions.Worker;

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
            new()
            {
                PEPRID = "producer-1",
                OrganisationName = "Producer 1",
                Status = "registered",
                OrganisationType = "DP",
                RegistrationYear = "2025"
            },
            new()
            {
                PEPRID = "producer-2",
                OrganisationName = "Producer 2",
                Status = "registered",
                OrganisationType = "CS",
                RegistrationYear = "2025"
            },
            new()
            {
                PEPRID = "producer-3",
                OrganisationName = "Producer 3",
                Status = "deleted",
                OrganisationType = "DP",
                RegistrationYear = "2024"
            }
        };

        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _commonDataService.Setup(x =>
                x.GetUpdatedProducersV2(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
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
}