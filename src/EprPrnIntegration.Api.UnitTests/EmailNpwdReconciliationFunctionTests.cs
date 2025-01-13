using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Service;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests;

public class EmailNpwdReconciliationFunctionTests
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAppInsightsService> _mockAppInsightsService;
    private readonly Mock<IOptions<FeatureManagementConfiguration>> _mockFeatureConfig;
    private readonly Mock<ILogger<EmailNpwdReconciliationFunction>> _mockLogger;

    private readonly EmailNpwdReconciliationFunction _function;

    public EmailNpwdReconciliationFunctionTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _mockAppInsightsService = new Mock<IAppInsightsService>();
        _mockFeatureConfig = new Mock<IOptions<FeatureManagementConfiguration>>();

        _mockLogger = new Mock<ILogger<EmailNpwdReconciliationFunction>>();

        _function = new EmailNpwdReconciliationFunction(_mockEmailService.Object,
            _mockAppInsightsService.Object,
            _mockFeatureConfig.Object,
            _mockLogger.Object);

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
        await _function.Run(new TimerInfo());

        // Assert
        _mockLogger.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.StartsWith("EmailNpwdReconciliation function is disabled by feature flag"))));
    }

    [Fact]
    public async Task Run_Calls_EmailNpwdIssuedPrnsReconciliationAsyn_When_Feature_Flag_Is_True()
    {
        // Arrange
        var config = new FeatureManagementConfiguration
        {
            RunIntegration = true
        };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _mockLogger.VerifyLog(x => x.LogInformation(It.Is<string>(s => s.StartsWith("EmailNpwdIssuedPrnsReconciliationAsync function executed"))));
    }

    [Fact]
    public async Task EmailNpwdIssuedPrnsReconciliationAsync_Sends_Email()
    {
        // Arrange
        var prns = new List<ReconcileIssuedPrn>();
        prns = new List<ReconcileIssuedPrn> { new ReconcileIssuedPrn { PrnNumber = "PRN1", PrnStatus = "ACCEPTED", UploadedDate = "10/01/2025", OrganisationName = "Sainsburys" } };

        _mockAppInsightsService.Setup(x => x.GetIssuedPrnCustomEventLogsLast24hrsAsync()).ReturnsAsync(prns);  

        // Act
        await _function.EmailNpwdIssuedPrnsReconciliationAsync();

        // Assert
        _mockEmailService.Verify(x => x.SendIssuedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), 1, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EmailNpwdIssuedPrnsReconciliationAsync_On_Exception_Logs_Error()
    {
        // Arrange
        _mockAppInsightsService.Setup(x => x.GetIssuedPrnCustomEventLogsLast24hrsAsync()).ThrowsAsync(new Exception());

        // Act
        await _function.EmailNpwdIssuedPrnsReconciliationAsync();

        // Assert
        _mockEmailService.Verify(x => x.SendIssuedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), 1, It.IsAny<string>()), Times.Never);
        _mockLogger.VerifyLog(x => x.LogError(It.Is<string>(s => s.StartsWith("Failed running EmailNpwdIssuedPrnsReconciliationAsync"))));
    }

    [Fact]
    public void TransformPrnsToCsv_GivenZeroRecords_ReturnsHeader()
    {
        // Arrange
        var prns = new List<ReconcileIssuedPrn>();

        // Act
       string csv = EmailNpwdReconciliationFunction.TransformPrnsToCsv(prns);

        // Accert
        csv.Should().Be(string.Concat("PRN Number,Incoming Status,Date,Organisation Name", Environment.NewLine));
    }

    [Fact]
    public void TransformPrnsToCsv_GivenRecords_ReturnsRows()
    {
        // Arrange
        var prns = new List<ReconcileIssuedPrn>();
        prns = new List<ReconcileIssuedPrn> { new ReconcileIssuedPrn { PrnNumber = "PRN1", PrnStatus = "ACCEPTED", UploadedDate = "10/01/2025", OrganisationName = "Sainsburys" } };

        // Act
        string csv = EmailNpwdReconciliationFunction.TransformPrnsToCsv(prns);

        // Accert
        csv.Should().StartWith(string.Concat("PRN Number,Incoming Status,Date,Organisation Name", Environment.NewLine));
        csv.Should().EndWith(string.Concat("PRN1,ACCEPTED,10/01/2025,Sainsburys", Environment.NewLine));
    }

}