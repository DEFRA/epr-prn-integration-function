using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Api;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EprPrnIntegration.Api.Tests;

public class GetPersonsEmailsFunctionTests
{
    private readonly Mock<ILogger<GetPersonsEmailsFunction>> _loggerMock = new();
    private readonly Mock<IOrganisationService> _organisationServiceMock = new();

    [Fact]
    public async Task Run_OrganisationIdMissing_ReturnsBadRequest()
    {
        // Arrange
        var function = new GetPersonsEmailsFunction(_loggerMock.Object, _organisationServiceMock.Object);

        var httpRequestMock = new Mock<HttpRequest>();
        httpRequestMock.Setup(req => req.Query["organisationId"]).Returns(string.Empty);

        // Act
        var result = await function.Run(httpRequestMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Organisation ID is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Run_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var organisationId = "12345";
        var responseEmails = new List<PersonEmail>
        {
            new PersonEmail { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
            new PersonEmail { FirstName = "Jane", LastName = "Doe", Email = "jane.doe@example.com" }
        };

        _organisationServiceMock
            .Setup(service => service.GetPersonEmailsAsync(organisationId, CancellationToken.None))
            .ReturnsAsync(responseEmails);

        var function = new GetPersonsEmailsFunction(_loggerMock.Object, _organisationServiceMock.Object);

        var httpRequestMock = new Mock<HttpRequest>();
        httpRequestMock.Setup(req => req.Query["organisationId"]).Returns(organisationId);

        // Act
        var result = await function.Run(httpRequestMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<List<PersonEmail>>(okResult.Value);
        Assert.Equal(2, actualResponse.Count);
        Assert.Equal("john.doe@example.com", actualResponse[0].Email);
        Assert.Equal("jane.doe@example.com", actualResponse[1].Email);
        _organisationServiceMock.Verify(service => service.GetPersonEmailsAsync(organisationId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Run_HttpRequestException_ReturnsServiceUnavailable()
    {
        // Arrange
        var organisationId = "12345";

        _organisationServiceMock
            .Setup(service => service.GetPersonEmailsAsync(organisationId, CancellationToken.None))
            .ThrowsAsync(new HttpRequestException());

        var function = new GetPersonsEmailsFunction(_loggerMock.Object, _organisationServiceMock.Object);

        var httpRequestMock = new Mock<HttpRequest>();
        httpRequestMock.Setup(req => req.Query["organisationId"]).Returns(organisationId);

        // Act
        var result = await function.Run(httpRequestMock.Object);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Run_UnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        var organisationId = "12345";

        _organisationServiceMock
            .Setup(service => service.GetPersonEmailsAsync(organisationId, CancellationToken.None))
            .ThrowsAsync(new Exception("Unexpected error"));

        var function = new GetPersonsEmailsFunction(_loggerMock.Object, _organisationServiceMock.Object);

        var httpRequestMock = new Mock<HttpRequest>();
        httpRequestMock.Setup(req => req.Query["organisationId"]).Returns(organisationId);

        // Act
        var result = await function.Run(httpRequestMock.Object);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }
}