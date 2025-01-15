using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.Service;
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
    private readonly Mock<IUtilities> _mockUtilities;
    private readonly Mock<IPrnService> _mockPrnService;

    private readonly EmailNpwdReconciliationFunction _function;

    public EmailNpwdReconciliationFunctionTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _mockAppInsightsService = new Mock<IAppInsightsService>();
        _mockFeatureConfig = new Mock<IOptions<FeatureManagementConfiguration>>();
        _mockUtilities = new Mock<IUtilities>();
        _mockPrnService = new Mock<IPrnService>();

        _mockLogger = new Mock<ILogger<EmailNpwdReconciliationFunction>>();

        _function = new EmailNpwdReconciliationFunction(_mockEmailService.Object,
            _mockAppInsightsService.Object,
            _mockFeatureConfig.Object,
            _mockLogger.Object,
            _mockUtilities.Object,
            _mockPrnService.Object);

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
    public async Task EmailNpwdIssuedPrnsReconciliationAsync_Sends_Email_Successfully()
    {
        // Arrange
        var prns = new List<ReconcileIssuedPrn>
        {
            new() { PrnNumber = "PRN1", PrnStatus = "ACCEPTED", UploadedDate = "10/01/2025", OrganisationName = "Sainsburys" },
            new() { PrnNumber = "PRN2", PrnStatus = "REJECTED", UploadedDate = "11/01/2025", OrganisationName = "Tesco" }
        };

        var csvData = new Dictionary<string, List<string>>
        {
            { CustomEventFields.PrnNumber, prns.Select(x => x.PrnNumber).ToList() },
            { CustomEventFields.IncomingStatus, prns.Select(x => x.PrnStatus).ToList() },
            { CustomEventFields.Date, prns.Select(x => x.UploadedDate).ToList() },
            { CustomEventFields.OrganisationName, prns.Select(x => x.OrganisationName).ToList() },
        };

        var csvContent = "Mock CSV Content";

        _mockAppInsightsService
            .Setup(service => service.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(prns);

        _mockUtilities
            .Setup(utilities => utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>()))
            .Returns(csvContent);

        // Act
        await _function.EmailNpwdIssuedPrnsReconciliationAsync();

        // Assert
        _mockEmailService.Verify(service =>
            service.SendIssuedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), prns.Count, csvContent), Times.Once);

        _mockLogger.VerifyLog(logger =>
            logger.LogInformation(It.Is<string>(s => s.Contains("EmailNpwdIssuedPrnsReconciliationAsync function executed"))), Times.Once);
    }

    [Fact]
    public async Task EmailNpwdIssuedPrnsReconciliationAsync_Handles_EmptyPrnList()
    {
        // Arrange
        var prns = new List<ReconcileIssuedPrn>();

        _mockAppInsightsService
            .Setup(service => service.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(prns);

        // Act
        await _function.EmailNpwdIssuedPrnsReconciliationAsync();

        // Assert
        _mockEmailService.Verify(service =>
            service.SendIssuedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), 0, It.IsAny<string>()), Times.Once);

        _mockLogger.VerifyLog(logger =>
            logger.LogInformation(It.Is<string>(s => s.Contains("EmailNpwdIssuedPrnsReconciliationAsync function executed"))), Times.Once);
    }

    [Fact]
    public async Task EmailNpwdIssuedPrnsReconciliationAsync_Logs_Error_On_Exception()
    {
        // Arrange
        _mockAppInsightsService
            .Setup(service => service.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ThrowsAsync(new Exception("Mock exception"));

        // Act
        await _function.EmailNpwdIssuedPrnsReconciliationAsync();

        // Assert
        _mockEmailService.Verify(service =>
            service.SendIssuedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);

        _mockLogger.VerifyLog(logger =>
            logger.LogError(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("Failed running EmailNpwdIssuedPrnsReconciliationAsync"))), Times.Once);
    }

    [Fact]
    public async Task EmailUpdatedPrnReconciliationAsync_Sends_Email_Successfully()
    {
        // Arrange
        var updatedPrns = new List<ReconcileUpdatedPrnsResponseModel>
        {
            new ReconcileUpdatedPrnsResponseModel { PrnNumber = "PRN1", StatusName = "APPROVED", UpdatedOn = "10/01/2025", OrganisationName = "Company A" },
            new ReconcileUpdatedPrnsResponseModel { PrnNumber = "PRN2", StatusName = "REJECTED", UpdatedOn = "11/01/2025", OrganisationName = "Company B" }
        };

        var csvData = new Dictionary<string, List<string>>
        {
            { CustomEventFields.PrnNumber, updatedPrns.Select(x => x.PrnNumber).ToList() },
            { CustomEventFields.IncomingStatus, updatedPrns.Select(x => x.StatusName).ToList() },
            { CustomEventFields.Date, updatedPrns.Select(x => x.UpdatedOn).ToList() },
            { CustomEventFields.OrganisationName, updatedPrns.Select(x => x.OrganisationName).ToList() },
        };

        var csvContent = "Mock CSV Content";

        _mockPrnService
            .Setup(service => service.GetReconsolidatedUpdatedPrns())
            .ReturnsAsync(updatedPrns);

        _mockUtilities
            .Setup(utilities => utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>()))
            .Returns(csvContent);

        // Act
        await _function.EmailUpdatedPrnReconciliationAsync();

        // Assert
        _mockEmailService.Verify(service =>
            service.SendUpdatedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), csvContent), Times.Once);

        _mockLogger.VerifyLog(logger =>
            logger.LogInformation(It.Is<string>(s => s.Contains("EmailUpdatedPrnReconciliationAsync function executed"))), Times.Once);
    }

    [Fact]
    public async Task EmailUpdatedPrnReconciliationAsync_Handles_EmptyPrnList()
    {
        // Arrange
        var updatedPrns = new List<ReconcileUpdatedPrnsResponseModel>();

        _mockPrnService
            .Setup(service => service.GetReconsolidatedUpdatedPrns())
            .ReturnsAsync(updatedPrns);

        // Act
        await _function.EmailUpdatedPrnReconciliationAsync();

        // Assert
        _mockEmailService.Verify(service =>
            service.SendUpdatedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), It.IsAny<string>()), Times.Once);

        _mockLogger.VerifyLog(logger =>
            logger.LogInformation(It.Is<string>(s => s.Contains("EmailUpdatedPrnReconciliationAsync function executed"))), Times.Once);
    }

    [Fact]
    public async Task EmailUpdatedPrnReconciliationAsync_Logs_Error_On_Exception()
    {
        // Arrange
        _mockPrnService
            .Setup(service => service.GetReconsolidatedUpdatedPrns())
            .ThrowsAsync(new Exception("Mock exception"));

        // Act
        await _function.EmailUpdatedPrnReconciliationAsync();

        // Assert
        _mockEmailService.Verify(service =>
            service.SendUpdatedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);

        _mockLogger.VerifyLog(logger =>
            logger.LogError(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("Failed running EmailUpdatedPrnReconciliationAsync"))), Times.Once);
    }

    [Fact]
    public async Task Run_Executes_Both_Tasks_Concurrently()
    {
        // Arrange
        var config = new FeatureManagementConfiguration { RunIntegration = true };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        var updatedPrns = new List<ReconcileUpdatedPrnsResponseModel>
        {
            new ReconcileUpdatedPrnsResponseModel { PrnNumber = "PRN1", StatusName = "APPROVED", UpdatedOn = "10/01/2025", OrganisationName = "Company A" }
        };

        var issuedPrns = new List<ReconcileIssuedPrn>
        {
            new ReconcileIssuedPrn { PrnNumber = "PRN2", PrnStatus = "ACCEPTED", UploadedDate = "11/01/2025", OrganisationName = "Company B" }
        };

        var csvContent = "Mock CSV Content";

        _mockPrnService
            .Setup(service => service.GetReconsolidatedUpdatedPrns())
            .ReturnsAsync(updatedPrns);

        _mockAppInsightsService
            .Setup(service => service.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(issuedPrns);

        _mockUtilities
            .Setup(utilities => utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>()))
            .Returns(csvContent);

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _mockEmailService.Verify(service =>
            service.SendUpdatedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), csvContent), Times.Once);

        _mockEmailService.Verify(service =>
            service.SendIssuedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), issuedPrns.Count, csvContent), Times.Once);

        _mockLogger.VerifyLog(logger =>
            logger.LogInformation(It.Is<string>(s => s.Contains("EmailUpdatedPrnReconciliationAsync function executed"))), Times.Once);

        _mockLogger.VerifyLog(logger =>
            logger.LogInformation(It.Is<string>(s => s.Contains("EmailNpwdIssuedPrnsReconciliationAsync function executed"))), Times.Once);
    }

    [Fact]
    public async Task Run_Handles_Exception_From_One_Task()
    {
        // Arrange
        var config = new FeatureManagementConfiguration { RunIntegration = true };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        var issuedPrns = new List<ReconcileIssuedPrn>
        {
            new ReconcileIssuedPrn { PrnNumber = "PRN1", PrnStatus = "ACCEPTED", UploadedDate = "11/01/2025", OrganisationName = "Company B" }
        };

        var csvContent = "Mock CSV Content";

        _mockPrnService
            .Setup(service => service.GetReconsolidatedUpdatedPrns())
            .ThrowsAsync(new Exception("Mock exception from Updated PRNs"));

        _mockAppInsightsService
            .Setup(service => service.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(issuedPrns);

        _mockUtilities
            .Setup(utilities => utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>()))
            .Returns(csvContent);

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _mockEmailService.Verify(service =>
            service.SendIssuedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), issuedPrns.Count, csvContent), Times.Once);

        _mockEmailService.Verify(service =>
            service.SendUpdatedPrnsReconciliationEmailToNpwd(It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);

        _mockLogger.VerifyLog(logger =>
            logger.LogError(It.Is<Exception>(ex => ex.Message.Contains("Mock exception from Updated PRNs")), It.Is<string>(s => s.Contains("Failed running EmailUpdatedPrnReconciliationAsync"))), Times.Once);
    }
}