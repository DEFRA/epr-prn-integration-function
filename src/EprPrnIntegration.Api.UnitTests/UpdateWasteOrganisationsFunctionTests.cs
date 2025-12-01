using EprPrnIntegration.Api.Functions;
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

    private readonly UpdateWasteOrganisationsFunction function;

    public UpdateWasteOrganisationsFunctionTests()
    {
        function = new UpdateWasteOrganisationsFunction(_lastUpdateServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RunsFunction()
    {
        _lastUpdateServiceMock.Setup(x => x.GetLastUpdate(It.IsAny<string>())).ReturnsAsync(DateTime.MinValue);
        _lastUpdateServiceMock.Setup(x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);

        await function.Run(new TimerInfo());
    }
}