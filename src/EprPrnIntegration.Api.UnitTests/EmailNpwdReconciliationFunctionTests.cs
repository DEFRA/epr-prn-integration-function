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
    private readonly Mock<INpwdPrnService> _mockPrnService;

    private readonly EmailNpwdReconciliationFunction _function;

    public EmailNpwdReconciliationFunctionTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _mockAppInsightsService = new Mock<IAppInsightsService>();
        _mockFeatureConfig = new Mock<IOptions<FeatureManagementConfiguration>>();
        _mockUtilities = new Mock<IUtilities>();
        _mockPrnService = new Mock<INpwdPrnService>();

        _mockLogger = new Mock<ILogger<EmailNpwdReconciliationFunction>>();

        _function = new EmailNpwdReconciliationFunction(
            _mockEmailService.Object,
            _mockAppInsightsService.Object,
            _mockFeatureConfig.Object,
            _mockLogger.Object,
            _mockUtilities.Object,
            _mockPrnService.Object
        );
    }

    [Fact]
    public async Task Run_Ends_When_Feature_Flag_Is_False()
    {
        // Arrange
        var config = new FeatureManagementConfiguration
        {
            RunIntegration = true,
            RunReconciliation = false,
        };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailNpwdReconciliation function(s) disabled by feature flag"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Run_Calls_EmailNpwdIssuedPrnsReconciliationAsyn_When_Feature_Flag_Is_True()
    {
        // Arrange
        var config = new FeatureManagementConfiguration
        {
            RunIntegration = false,
            RunReconciliation = true,
        };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailNpwdIssuedPrnsReconciliationAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailNpwdIssuedPrnsReconciliationAsync_Sends_Email()
    {
        // Arrange
        var prns = new List<ReconcileIssuedPrn>();
        prns =
        [
            new ReconcileIssuedPrn
            {
                PrnNumber = "PRN1",
                PrnStatus = "ACCEPTED",
                UploadedDate = "10/01/2025",
                OrganisationName = "Sainsburys",
            },
        ];

        _mockAppInsightsService
            .Setup(x => x.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(prns);

        // Act
        await _function.EmailNpwdIssuedPrnsReconciliationAsync();

        // Assert
        _mockEmailService.Verify(
            x =>
                x.SendIssuedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    1,
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailNpwdIssuedPrnsReconciliationAsync_On_Exception_Logs_Error()
    {
        // Arrange
        _mockAppInsightsService
            .Setup(x => x.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ThrowsAsync(new Exception());

        // Act
        await _function.EmailNpwdIssuedPrnsReconciliationAsync();

        // Assert
        _mockEmailService.Verify(
            x =>
                x.SendIssuedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    1,
                    It.IsAny<string>()
                ),
            Times.Never
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "Failed running EmailNpwdIssuedPrnsReconciliationAsync"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailNpwdIssuedPrnsReconciliationAsync_Sends_Email_Successfully()
    {
        // Arrange
        var prns = new List<ReconcileIssuedPrn>
        {
            new()
            {
                PrnNumber = "PRN1",
                PrnStatus = "ACCEPTED",
                UploadedDate = "10/01/2025",
                OrganisationName = "Sainsburys",
            },
            new()
            {
                PrnNumber = "PRN2",
                PrnStatus = "REJECTED",
                UploadedDate = "11/01/2025",
                OrganisationName = "Tesco",
            },
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
            .Setup(utilities =>
                utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>())
            )
            .Returns(csvContent);

        // Act
        await _function.EmailNpwdIssuedPrnsReconciliationAsync();

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendIssuedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    prns.Count,
                    csvContent
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailNpwdIssuedPrnsReconciliationAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
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
        _mockEmailService.Verify(
            service =>
                service.SendIssuedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    0,
                    It.IsAny<string>()
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailNpwdIssuedPrnsReconciliationAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
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
        _mockEmailService.Verify(
            service =>
                service.SendIssuedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()
                ),
            Times.Never
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "Failed running EmailNpwdIssuedPrnsReconciliationAsync"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailUpdatedPrnReconciliationAsync_Sends_Email_Successfully()
    {
        // Arrange
        var updatedPrns = new List<ReconcileUpdatedNpwdPrnsResponseModel>
        {
            new ReconcileUpdatedNpwdPrnsResponseModel
            {
                PrnNumber = "PRN1",
                StatusName = "APPROVED",
                UpdatedOn = "10/01/2025",
                OrganisationName = "Company A",
            },
            new ReconcileUpdatedNpwdPrnsResponseModel
            {
                PrnNumber = "PRN2",
                StatusName = "REJECTED",
                UpdatedOn = "11/01/2025",
                OrganisationName = "Company B",
            },
        };

        var csvData = new Dictionary<string, List<string>>
        {
            { CustomEventFields.PrnNumber, updatedPrns.Select(x => x.PrnNumber).ToList() },
            { CustomEventFields.IncomingStatus, updatedPrns.Select(x => x.StatusName).ToList() },
            { CustomEventFields.Date, updatedPrns.Select(x => x.UpdatedOn).ToList() },
            {
                CustomEventFields.OrganisationName,
                updatedPrns.Select(x => x.OrganisationName).ToList()
            },
        };

        var csvContent = "Mock CSV Content";

        _mockPrnService
            .Setup(service => service.GetReconciledUpdatedNpwdPrns())
            .ReturnsAsync(updatedPrns);

        _mockUtilities
            .Setup(utilities =>
                utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>())
            )
            .Returns(csvContent);

        // Act
        await _function.EmailUpdatedPrnReconciliationAsync();

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendUpdatedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    csvContent,
                    updatedPrns.Count
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailUpdatedPrnReconciliationAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailUpdatedPrnReconciliationAsync_Handles_EmptyPrnList()
    {
        // Arrange
        var updatedPrns = new List<ReconcileUpdatedNpwdPrnsResponseModel>();

        _mockPrnService
            .Setup(service => service.GetReconciledUpdatedNpwdPrns())
            .ReturnsAsync(updatedPrns);

        // Act
        await _function.EmailUpdatedPrnReconciliationAsync();

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendUpdatedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    updatedPrns.Count
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailUpdatedPrnReconciliationAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailUpdatedPrnReconciliationAsync_Logs_Error_On_Exception()
    {
        // Arrange
        _mockPrnService
            .Setup(service => service.GetReconciledUpdatedNpwdPrns())
            .ThrowsAsync(new Exception("Mock exception"));

        // Act
        await _function.EmailUpdatedPrnReconciliationAsync();

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendUpdatedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()
                ),
            Times.Never
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "Failed running EmailUpdatedPrnReconciliationAsync"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Run_Executes_All_Tasks()
    {
        // Arrange
        var config = new FeatureManagementConfiguration
        {
            RunIntegration = true,
            RunReconciliation = true,
        };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        var updatedPrns = new List<ReconcileUpdatedNpwdPrnsResponseModel>
        {
            new()
            {
                PrnNumber = "PRN1",
                StatusName = "APPROVED",
                UpdatedOn = "10/01/2025",
                OrganisationName = "Company A",
            },
        };

        var issuedPrns = new List<ReconcileIssuedPrn>
        {
            new()
            {
                PrnNumber = "PRN2",
                PrnStatus = "ACCEPTED",
                UploadedDate = "11/01/2025",
                OrganisationName = "Company B",
            },
        };

        var updatedOrganisations = new List<UpdatedOrganisationReconciliationSummary>
        {
            new()
            {
                Id = "asa-23jasd-232-123900",
                Name = "Gen Test Ltd",
                Date = "13/01/2025",
                Address = "233 Henz Boulevard, Milton Keynes MK9 1AA",
            },
        };

        var csvContent = "Mock CSV Content";

        _mockPrnService
            .Setup(service => service.GetReconciledUpdatedNpwdPrns())
            .ReturnsAsync(updatedPrns);

        _mockAppInsightsService
            .Setup(service => service.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(issuedPrns);

        _mockAppInsightsService
            .Setup(service => service.GetUpdatedOrganisationsCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(updatedOrganisations);

        _mockUtilities
            .Setup(utilities =>
                utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>())
            )
            .Returns(csvContent);

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendUpdatedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    csvContent,
                    updatedPrns.Count
                ),
            Times.Once
        );

        _mockEmailService.Verify(
            service =>
                service.SendIssuedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    issuedPrns.Count,
                    csvContent
                ),
            Times.Once
        );

        _mockEmailService.Verify(
            service =>
                service.SendUpdatedOrganisationsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    updatedOrganisations.Count,
                    csvContent
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailUpdatedPrnReconciliationAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailNpwdIssuedPrnsReconciliationAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailNpwdUpdatedOrganisationsAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Run_Handles_Exception_From_One_Task()
    {
        // Arrange
        var config = new FeatureManagementConfiguration
        {
            RunIntegration = true,
            RunReconciliation = true,
        };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

        var issuedPrns = new List<ReconcileIssuedPrn>
        {
            new()
            {
                PrnNumber = "PRN1",
                PrnStatus = "ACCEPTED",
                UploadedDate = "11/01/2025",
                OrganisationName = "Company B",
            },
        };

        var csvContent = "Mock CSV Content";

        _mockPrnService
            .Setup(service => service.GetReconciledUpdatedNpwdPrns())
            .ThrowsAsync(new Exception("Mock exception from Updated PRNs"));

        _mockAppInsightsService
            .Setup(service => service.GetIssuedPrnCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(issuedPrns);

        _mockAppInsightsService
            .Setup(service => service.GetUpdatedOrganisationsCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(new List<UpdatedOrganisationReconciliationSummary>());

        _mockUtilities
            .Setup(utilities =>
                utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>())
            )
            .Returns(csvContent);

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendIssuedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    issuedPrns.Count,
                    csvContent
                ),
            Times.Once
        );

        _mockEmailService.Verify(
            service =>
                service.SendUpdatedPrnsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()
                ),
            Times.Never
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "Failed running EmailUpdatedPrnReconciliationAsync"
                            )
                    ),
                    It.Is<Exception>(ex => ex.Message.Contains("Mock exception from Updated PRNs")),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailNpwdUpdatedOrganisationsAsync_Sends_Email_Successfully()
    {
        // Arrange
        var updatedOrgs = new List<UpdatedOrganisationReconciliationSummary>
        {
            new()
            {
                Id = "asd-90-909asd-2323",
                Name = "Gen Test 1 Ltd",
                Date = "10/01/2025",
                Address = "122 Ashwood Park, London SE1 5AA",
                CompanyRegNo = "ABN2233",
                OrganisationType = "Producer",
                PEPRId = "29023788",
                Status = "Updated",
            },
            new()
            {
                Id = "aa-22-333-asda2",
                Name = "Gen Test 2 Ltd",
                Date = "11/01/2025",
                Address = "2331 Wide Lane North, Glasgow GL1 2AA",
                CompanyRegNo = "DES1122",
                OrganisationType = "Waste Management",
                PEPRId = "1788299",
                Status = "Updated",
            },
        };

        var csvData = new Dictionary<string, List<string>>
        {
            {
                CustomEventFields.OrganisationId,
                updatedOrgs.Select(x => x.Id ?? string.Empty).ToList()
            },
            {
                CustomEventFields.OrganisationName,
                updatedOrgs.Select(x => x.Name ?? string.Empty).ToList()
            },
            { CustomEventFields.Date, updatedOrgs.Select(x => x.Date ?? string.Empty).ToList() },
            {
                CustomEventFields.OrganisationAddress,
                updatedOrgs.Select(x => x.Address ?? string.Empty).ToList()
            },
            {
                CustomEventFields.OrganisationType,
                updatedOrgs.Select(x => x.OrganisationType ?? string.Empty).ToList()
            },
            {
                CustomEventFields.OrganisationStatus,
                updatedOrgs.Select(x => x.Status ?? string.Empty).ToList()
            },
            {
                CustomEventFields.OrganisationEprId,
                updatedOrgs.Select(x => x.PEPRId ?? string.Empty).ToList()
            },
            {
                CustomEventFields.OrganisationRegNo,
                updatedOrgs.Select(x => x.CompanyRegNo ?? string.Empty).ToList()
            },
        };

        var csvContent = "Mock CSV Content";

        _mockAppInsightsService
            .Setup(service => service.GetUpdatedOrganisationsCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(updatedOrgs);

        _mockUtilities
            .Setup(utilities =>
                utilities.CreateCsvContent(It.IsAny<Dictionary<string, List<string>>>())
            )
            .Returns(csvContent);

        // Act
        await _function.EmailNpwdUpdatedOrganisationsAsync();

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendUpdatedOrganisationsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    updatedOrgs.Count,
                    csvContent
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailNpwdUpdatedOrganisationsAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailNpwdUpdatedOrganisationsAsync_Handles_EmptyOrganisationList()
    {
        // Arrange
        var updatedOrgs = new List<UpdatedOrganisationReconciliationSummary>();

        _mockAppInsightsService
            .Setup(service => service.GetUpdatedOrganisationsCustomEventLogsLast24hrsAsync())
            .ReturnsAsync(updatedOrgs);

        // Act
        await _function.EmailNpwdUpdatedOrganisationsAsync();

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendUpdatedOrganisationsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    updatedOrgs.Count,
                    It.IsAny<string>()
                ),
            Times.Once
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "EmailNpwdUpdatedOrganisationsAsync function executed"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task EmailNpwdUpdatedOrganisationsAsync_Logs_Error_On_Exception()
    {
        // Arrange
        _mockAppInsightsService
            .Setup(service => service.GetUpdatedOrganisationsCustomEventLogsLast24hrsAsync())
            .ThrowsAsync(new Exception("Mock exception"));

        // Act
        await _function.EmailNpwdUpdatedOrganisationsAsync();

        // Assert
        _mockEmailService.Verify(
            service =>
                service.SendUpdatedOrganisationsReconciliationEmailToNpwd(
                    It.IsAny<DateTime>(),
                    0,
                    It.IsAny<string>()
                ),
            Times.Never
        );

        _mockLogger.Verify(
            logger =>
                logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, type) =>
                            ContainsString(
                                state,
                                "Failed running EmailNpwdUpdatedOrganisationsAsync"
                            )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.Once
        );
    }

    private static bool ContainsString(object obj, string value)
    {
        return obj?.ToString()?.Contains(value) == true;
    }
}
