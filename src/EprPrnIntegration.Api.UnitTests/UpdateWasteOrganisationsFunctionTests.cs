using EprPrnIntegration.Api.Functions;
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

    private readonly UpdateWasteOrganisationsFunction function;

    public UpdateWasteOrganisationsFunctionTests()
    {
        function = new UpdateWasteOrganisationsFunction(_lastUpdateServiceMock.Object, _loggerMock.Object, _commonDataService.Object, _wasteOrganisationsService.Object);
    }

    [Fact]
    public async Task RunsFunction()
    {
        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        _commonDataService.Setup(x =>
                x.GetUpdatedProducersV2(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
                
        await function.Run(new TimerInfo());
        
        _lastUpdateServiceMock.Verify(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }
}