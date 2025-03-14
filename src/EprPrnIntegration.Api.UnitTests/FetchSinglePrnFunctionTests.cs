﻿using System.Net;
using System.Text;
using System.Text.Json;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Client;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests;

public class FetchSinglePrnFunctionTests
{
    private readonly Mock<IServiceBusProvider> _serviceBusProviderMock = new();
    private readonly Mock<INpwdClient> _npwdClientMock = new();
    private readonly Mock<ILogger<FetchSinglePrnFunction>> _loggerMock = new();
    private readonly FetchSinglePrnFunction _function;

    public FetchSinglePrnFunctionTests()
    {
        _function = new FetchSinglePrnFunction(_serviceBusProviderMock.Object, _npwdClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Run_ReturnsBadRequest_WhenPrnNumberIsNull()
    {
        // Arrange
        var request = new DefaultHttpContext().Request;
        request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new { PrnNumber = (string?)null }));

        // Act
        var result = await _function.Run(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please provide a valid PRN number.", badRequestResult.Value);
    }

    [Fact]
    public async Task Run_ReturnsNotFound_WhenPrnNotFound()
    {
        // Arrange
        var prnNumber = "ER12345678";
        var request = new DefaultHttpContext().Request;

        var jsonPayload = JsonSerializer.Serialize(new FetchSinglePrnRequest { PrnNumber = prnNumber });
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
        requestBodyStream.Seek(0, SeekOrigin.Begin);
        request.Body = requestBodyStream;
        request.ContentType = "application/json";

        _npwdClientMock.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
            .ReturnsAsync(new List<NpwdPrn>());

        // Act
        var result = await _function.Run(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"{prnNumber} is not found in NPWD system.", notFoundResult.Value);
    }

    [Fact]
    public async Task Run_ReturnsOk_WhenPrnIsFoundAndAddedToQueue()
    {
        // Arrange
        var prnNumber = "ER12345678";
        var request = new DefaultHttpContext().Request;
        
        var jsonPayload = JsonSerializer.Serialize(new FetchSinglePrnRequest { PrnNumber = prnNumber });
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
        requestBodyStream.Seek(0, SeekOrigin.Begin);
        request.Body = requestBodyStream;
        request.ContentType = "application/json";

        var fetchedPrn = new NpwdPrn { EvidenceNo = prnNumber };
        _npwdClientMock.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
            .ReturnsAsync(new List<NpwdPrn> { fetchedPrn });

        // Act
        var result = await _function.Run(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal($"{prnNumber} is produced to the queue to be processed.", okResult.Value);
        _serviceBusProviderMock.Verify(provider => provider.SendFetchedNpwdPrnsToQueue(It.Is<List<NpwdPrn>>(prns => prns.Contains(fetchedPrn))), Times.Once);
    }

    [Fact]
    public async Task Run_ReturnsInternalServerError_OnException()
    {
        // Arrange
        var prnNumber = "ER12345678";
        var request = new DefaultHttpContext().Request;

        var jsonPayload = JsonSerializer.Serialize(new FetchSinglePrnRequest { PrnNumber = prnNumber });
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
        requestBodyStream.Seek(0, SeekOrigin.Begin);
        request.Body = requestBodyStream;
        request.ContentType = "application/json";

        _npwdClientMock.Setup(client => client.GetIssuedPrns(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _function.Run(request);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, statusCodeResult.StatusCode);
    }
}