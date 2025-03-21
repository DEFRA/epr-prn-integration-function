﻿using AutoFixture;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Models.Queues;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.Service;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;
using EprPrnIntegration.Common.Constants;
using FluentAssertions;

namespace EprPrnIntegration.Api.UnitTests;

public class UpdatePrnsFunctionTests
{
    private readonly Mock<IPrnService> _mockPrnService;
    private readonly Mock<INpwdClient> _mockNpwdClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<UpdatePrnsFunction>> _loggerMock;
    private Mock<IOptions<FeatureManagementConfiguration>> _mockFeatureConfig;
    private Mock<IUtilities> _mockUtilities;
    private readonly Fixture _fixture = new();
    private readonly Mock<IEmailService> _emailService;
    private UpdatePrnsFunction _function;

    public UpdatePrnsFunctionTests()
    {
        _mockPrnService = new Mock<IPrnService>();
        _mockNpwdClient = new Mock<INpwdClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<UpdatePrnsFunction>>();
        _mockFeatureConfig = new Mock<IOptions<FeatureManagementConfiguration>>();
        _mockUtilities = new Mock<IUtilities>();
        _emailService = new Mock<IEmailService>();

        _function = new UpdatePrnsFunction(
            _mockPrnService.Object,
            _mockNpwdClient.Object,
            _loggerMock.Object,
            _mockConfiguration.Object,
            _mockFeatureConfig.Object,
            _mockUtilities.Object,
            _emailService.Object
        );

        // Turn the feature flag on
        var config = new FeatureManagementConfiguration
        {
            RunIntegration = true
        };
        _mockFeatureConfig.Setup(c => c.Value).Returns(config);

    }


    [Fact]
    public async Task Run_ShouldLogWarning_WhenNoUpdatedPrnsRetrieved()
    {
        // Arrange
        _mockUtilities
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatePrns,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });


        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedPrnsResponseModel>());

        // Act
        await _function.Run(null!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("No updated Prns are retrieved from common database")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldLogSuccess_WhenPrnListUpdatedSuccessfully()
    {
        // Arrange
        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _mockUtilities
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatePrns,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        // Act
        await _function.Run(null!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Prns list successfully updated in NPWD")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldLogError_WhenPrnListUpdateFails()
    {
        // Arrange
        _mockUtilities
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatePrns,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        await _function.Run(null!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Failed to update Prns list in NPWD")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Status Code: BadRequest")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldLogError_WhenPrnServiceThrowsException()
    {
        // Arrange
        _mockUtilities
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatePrns,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        await _function.Run(null!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Failed to retrieve data from common backend")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("form time period")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
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
        _loggerMock.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("UpdatePrnsList function is disabled by feature flag")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
        ), Times.Once);
    }

    [Fact]
    public async Task Run_SendsDeltaSyncExecutionToQueue()
    {
        // Arrange
        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _mockUtilities
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatePrns,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });


        // Mock DeltaSyncExecution
        var deltaSyncExecution = new DeltaSyncExecution
        {
            SyncType = NpwdDeltaSyncType.UpdatePrns,
            LastSyncDateTime = DateTime.UtcNow.AddHours(-1)
        };

        _mockUtilities
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns))
            .ReturnsAsync(deltaSyncExecution);

        // Act
        await _function.Run(null!);

        // Assert
        _mockUtilities.Verify(
            provider => provider.SetDeltaSyncExecution(
                It.Is<DeltaSyncExecution>(d => d.SyncType == NpwdDeltaSyncType.UpdatePrns), It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_NoMessageInQueue_UsesDefaultFromConfig()
    {
        // Arrange
        var defaultDatetime = "2024-01-01";

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _mockUtilities
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns))
            .ReturnsAsync(new DeltaSyncExecution
            {
                LastSyncDateTime = DateTime.Parse(defaultDatetime),
                SyncType = NpwdDeltaSyncType.UpdatePrns
            });

        // Act
        await _function.Run(null!);

        // Assert: Verify that DeltaSyncExecution is created using the default date from config
        _mockPrnService.Verify(service =>
            service.GetUpdatedPrns(DateTime.Parse(defaultDatetime), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_CallsInsertSyncData_WhenPatchToNpwdIsSuccessful()
    {

        var deltaRun = _fixture.Create<DeltaSyncExecution>();
        var updatePrns = _fixture.CreateMany<UpdatedPrnsResponseModel>().ToList();

        _mockUtilities.Setup(m => m.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>()))
            .ReturnsAsync(deltaRun);

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(updatePrns);

        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _function.Run(default!);

        _mockPrnService.Verify(service =>
            service.InsertPeprNpwdSyncPrns(It.IsAny<IEnumerable<UpdatedPrnsResponseModel>>()), Times.Once);
    }

    [Fact]
    public async Task Run_NoCallsToInsertSyncData_WhenPatchToNpwdFails()
    {

        var deltaRun = _fixture.Create<DeltaSyncExecution>();
        var updatePrns = _fixture.CreateMany<UpdatedPrnsResponseModel>().ToList();

        _mockUtilities.Setup(m => m.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>()))
            .ReturnsAsync(deltaRun);

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(updatePrns);

        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));

        // Act
        await _function.Run(default!);

        // Assert: Verify that DeltaSyncExecution is created using the default date from config
        _mockPrnService.Verify(service =>
            service.InsertPeprNpwdSyncPrns(It.IsAny<IEnumerable<UpdatedPrnsResponseModel>>()), Times.Never);
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.InternalServerError)]
    [InlineData(System.Net.HttpStatusCode.RequestTimeout)]
    [InlineData(System.Net.HttpStatusCode.GatewayTimeout)]
    public async Task Run_SendsErrorEmail_EmailToNpwd_When_ServerSide_Error_Occurs(System.Net.HttpStatusCode statusCode)
    {
        // Arrange
        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpdatedPrnsResponseModel> { new UpdatedPrnsResponseModel { EvidenceNo = "123", EvidenceStatusCode = "Active" } });

        _mockUtilities
            .Setup(provider => provider.GetDeltaSyncExecution(NpwdDeltaSyncType.UpdatePrns))
            .ReturnsAsync(new DeltaSyncExecution
            {
                SyncType = NpwdDeltaSyncType.UpdatePrns,
                LastSyncDateTime = DateTime.UtcNow.AddHours(-1) // Set last sync date
            });

        _mockNpwdClient.Setup(x => x.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("Server Error")});
            

        // Act
        await _function.Run(null!);

        // Assert
        _emailService.Verify(x => x.SendErrorEmailToNpwd(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Run_AddCustomEventForUpdatePrns()
    {
        // Arrange
        var deltaRun = _fixture.Create<DeltaSyncExecution>();
        var updatePrns = _fixture.CreateMany<UpdatedPrnsResponseModel>().ToList();

        _mockUtilities.Setup(m => m.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>()))
            .ReturnsAsync(deltaRun);

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(updatePrns);

        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _function.Run(new());

        _mockUtilities.Verify(u => u.AddCustomEvent(It.Is<string>(s => s == CustomEvents.UpdatePrn),
            It.Is<Dictionary<string, string>>(
                data => data["EvidenceNo"] == updatePrns[0].EvidenceNo
                && data["EvidenceStatusCode"] == updatePrns[0].EvidenceStatusCode
                && data["StatusDate"] == updatePrns[0].StatusDate.GetValueOrDefault().ToUniversalTime().ToString())), Times.Once);
    }

    [Fact]
    public async Task Run_BatchesPrns_WhenNumberOfPrnsExceedsBatchSize()
    {
        _mockConfiguration
            .Setup(config => config["UpdatePrnsMaxRows"])
            .Returns("2");

        DeltaSyncExecution deltaRun = new() { SyncType = NpwdDeltaSyncType.UpdatePrns, LastSyncDateTime = DateTime.UtcNow };

        var firstPrn = new UpdatedPrnsResponseModel
        {
            EvidenceNo = "A",
            EvidenceStatusCode = "ACCEPTED",
            StatusDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(2))
        };

        var secondPrn = new UpdatedPrnsResponseModel
        {
            EvidenceNo = "B",
            EvidenceStatusCode = "ACCEPTED",
            StatusDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1))
        };

        var thirdPrn = new UpdatedPrnsResponseModel
        {
            EvidenceNo = "C",
            EvidenceStatusCode = "ACCEPTED",
            StatusDate = DateTime.UtcNow
        };

        var updatePrns = new List<UpdatedPrnsResponseModel> { firstPrn, secondPrn, thirdPrn };

        _mockUtilities.Setup(m => m.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>()))
            .ReturnsAsync(deltaRun);

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(updatePrns);

        int prnCount = 0;
        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback((PrnDelta delta, string v) => prnCount = delta.Value.Count);

        // Act
        await _function.Run(default!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Batching 2 of 3 Prns")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        prnCount.Should().Be(2);

        _mockUtilities.Verify(
            provider => provider.SetDeltaSyncExecution(
                It.Is<DeltaSyncExecution>(d => d.SyncType == NpwdDeltaSyncType.UpdatePrns), secondPrn.StatusDate.Value),
            Times.Once);

    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Run_DoesNotBatchPrns_WhenNumberOfPrnsLessThanOrEqualToBatchSize(int countOfPrnsReturned)
    {
        _mockConfiguration
            .Setup(config => config["UpdatePrnsMaxRows"])
            .Returns("3");

        DeltaSyncExecution deltaRun = new() { SyncType = NpwdDeltaSyncType.UpdatePrns, LastSyncDateTime = DateTime.UtcNow };

        var updatePrns = _fixture.CreateMany<UpdatedPrnsResponseModel>(countOfPrnsReturned).ToList();

        _mockUtilities.Setup(m => m.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>()))
            .ReturnsAsync(deltaRun);

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(updatePrns);

        int prnCount = 0;
        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback((PrnDelta delta, string v) => prnCount = delta.Value.Count);

        // Act
        await _function.Run(default!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Batching")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);

        prnCount.Should().Be(countOfPrnsReturned);

    }

    [Fact]
    public async Task Run_DoesNotBatchPrns_WhenBatchSizeIsZero()
    {
        _mockConfiguration
            .Setup(config => config["UpdatePrnsMaxRows"])
            .Returns("0");

        DeltaSyncExecution deltaRun = new() { SyncType = NpwdDeltaSyncType.UpdatePrns, LastSyncDateTime = DateTime.UtcNow };

        var updatePrns = _fixture.CreateMany<UpdatedPrnsResponseModel>(3).ToList();

        _mockUtilities.Setup(m => m.GetDeltaSyncExecution(It.IsAny<NpwdDeltaSyncType>()))
            .ReturnsAsync(deltaRun);

        _mockPrnService.Setup(s => s.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(updatePrns);

        int prnCount = 0;
        _mockNpwdClient.Setup(c => c.Patch(It.IsAny<PrnDelta>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback((PrnDelta delta, string v) => prnCount = delta.Value.Count);

        // Act
        await _function.Run(default!);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => $"{v}".ToString().Contains("Batching")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);

        prnCount.Should().Be(3);

    }
}