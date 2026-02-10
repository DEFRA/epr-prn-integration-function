using System.Net;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests.Functions;

public class HttpHelperTests
{
    private readonly Mock<ILogger> _loggerMock;

    public HttpHelperTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    #region HandleTransientErrors Tests

    [Fact]
    public async Task HandleTransientErrors_ReturnsTrue_WhenResponseIsSuccess()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        Task<HttpResponseMessage> action(CancellationToken _) => Task.FromResult(response);
        var message = "Test operation";

        // Act
        var result = await HttpHelper.HandleTransientErrors(
            action,
            _loggerMock.Object,
            message,
            shouldNotContinueOn: [],
            CancellationToken.None
        );

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"{message} - success")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleTransientErrors_ReturnsFalse_WhenActionThrowsException()
    {
        // Arrange
        var exception = new Exception("Test exception");
        Task<HttpResponseMessage> action(CancellationToken _) => throw exception;
        var message = "Test operation";

        // Act
        var result = await HttpHelper.HandleTransientErrors(
            action,
            _loggerMock.Object,
            message,
            shouldNotContinueOn: [],
            CancellationToken.None
        );

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains($"{message} - exception, continuing with next")
                    ),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleTransientErrors_ThrowsServiceException_WhenTransientErrorOccurs()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        Task<HttpResponseMessage> action(CancellationToken _) => Task.FromResult(response);
        var message = "Test operation";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            HttpHelper.HandleTransientErrors(
                action,
                _loggerMock.Object,
                message,
                shouldNotContinueOn: [],
                CancellationToken.None
            )
        );

        Assert.Contains("transient error", exception.Message);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);

        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains($"{message} - transient error")
                            && v.ToString()!.Contains("terminating")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task HandleTransientErrors_ThrowsServiceException_ForAllTransientStatusCodes(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        Task<HttpResponseMessage> action(CancellationToken _) => Task.FromResult(response);
        var message = "Test operation";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            HttpHelper.HandleTransientErrors(
                action,
                _loggerMock.Object,
                message,
                shouldNotContinueOn: [],
                CancellationToken.None
            )
        );

        Assert.Equal(statusCode, exception.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task HandleTransientErrors_ReturnsFalse_ForNonTransientErrors(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        Task<HttpResponseMessage> action(CancellationToken _) => Task.FromResult(response);
        var message = "Test operation";

        // Act
        var result = await HttpHelper.HandleTransientErrors(
            action,
            _loggerMock.Object,
            message,
            shouldNotContinueOn: [],
            CancellationToken.None
        );

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains($"{message} - non transient error")
                            && v.ToString()!.Contains("continuing with next")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleTransientErrors_PassesCancellationToken_ToAction()
    {
        // Arrange
        CancellationToken? capturedToken = null;
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        Task<HttpResponseMessage> action(CancellationToken ct)
        {
            capturedToken = ct;
            return Task.FromResult(response);
        }
        var cts = new CancellationTokenSource();
        var message = "Test operation";

        // Act
        await HttpHelper.HandleTransientErrors(action, _loggerMock.Object, message, shouldNotContinueOn: [], cts.Token);

        // Assert
        Assert.NotNull(capturedToken);
        Assert.Equal(cts.Token, capturedToken.Value);
    }

    [Fact]
    public async Task HandleTransientErrors_WhenTaskCanceledException_Throws()
    {
        Task<HttpResponseMessage> action(CancellationToken _) => throw new TaskCanceledException();
        var message = "Test operation";

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            HttpHelper.HandleTransientErrors(action, _loggerMock.Object, message, shouldNotContinueOn: [],
                CancellationToken.None));
    }

    #endregion

    #region HandleTransientErrorsGet Tests

    [Fact]
    public async Task HandleTransientErrorsGet_ReturnsDeserializedContent_WhenResponseIsSuccess()
    {
        // Arrange
        var testObject = new TestModel { Id = 1, Name = "Test" };
        var jsonContent = JsonConvert.SerializeObject(testObject);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent),
        };
        Func<CancellationToken, Task<HttpResponseMessage>> action = _ => Task.FromResult(response);
        var message = "Test operation";

        // Act
        var result = await HttpHelper.HandleTransientErrorsGet<TestModel>(
            action,
            _loggerMock.Object,
            message,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testObject.Id, result.Id);
        Assert.Equal(testObject.Name, result.Name);
    }

    [Fact]
    public async Task HandleTransientErrorsGet_ReturnsNull_WhenResponseIsNotSuccess()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        Task<HttpResponseMessage> action(CancellationToken _) => Task.FromResult(response);
        var message = "Test operation";

        // Act
        var result = await HttpHelper.HandleTransientErrorsGet<TestModel>(
            action,
            _loggerMock.Object,
            message,
            CancellationToken.None
        );

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleTransientErrorsGet_ThrowsServiceException_WhenTransientErrorOccurs()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        Task<HttpResponseMessage> action(CancellationToken _) => Task.FromResult(response);
        var message = "Test operation";

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() =>
            HttpHelper.HandleTransientErrorsGet<TestModel>(
                action,
                _loggerMock.Object,
                message,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task HandleTransientErrorsGet_ReturnsNull_WhenActionThrowsException()
    {
        // Arrange
        static Task<HttpResponseMessage> action(CancellationToken _) =>
            throw new Exception("Test exception");
        var message = "Test operation";

        // Act
        var result = await HttpHelper.HandleTransientErrorsGet<TestModel>(
            action,
            _loggerMock.Object,
            message,
            CancellationToken.None
        );

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region HttpResponseMessageExtensions Tests

    [Fact]
    public async Task GetContent_ReturnsDeserializedObject_WhenResponseIsSuccessWithValidContent()
    {
        // Arrange
        var testObject = new TestModel { Id = 123, Name = "Test Object" };
        var jsonContent = JsonConvert.SerializeObject(testObject);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent),
        };

        // Act
        var result = await response.GetContent<TestModel>(_loggerMock.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testObject.Id, result.Id);
        Assert.Equal(testObject.Name, result.Name);
    }

    [Fact]
    public async Task GetContent_ReturnsNull_WhenResponseIsNotSuccess()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"id\": 1}"),
        };

        // Act
        var result = await response.GetContent<TestModel>(_loggerMock.Object);

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("Cannot get content")
                            && v.ToString()!.Contains("status is not success")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetContent_ReturnsNull_WhenContentIsEmpty()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(""),
        };

        // Act
        var result = await response.GetContent<TestModel>(_loggerMock.Object);

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("Expected content")
                            && v.ToString()!.Contains("but got none")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetContent_ReturnsNull_WhenContentIsWhitespace()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("   "),
        };

        // Act
        var result = await response.GetContent<TestModel>(_loggerMock.Object);

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("Expected content")
                            && v.ToString()!.Contains("but got none")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetContent_ReturnsNull_WhenDeserializationReturnsNull()
    {
        // Arrange
        // Using "null" as JSON content will deserialize to null
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null"),
        };

        // Act
        var result = await response.GetContent<TestModel>(_loggerMock.Object);

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("Failed to deserialize content")
                            && v.ToString()!.Contains("TestModel")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetContent_UsesCancellationToken_WhenProvided()
    {
        // Arrange
        var testObject = new TestModel { Id = 1, Name = "Test" };
        var jsonContent = JsonConvert.SerializeObject(testObject);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent),
        };
        var cts = new CancellationTokenSource();

        // Act
        var result = await response.GetContent<TestModel>(_loggerMock.Object, cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetContent_UsesDefaultCancellationToken_WhenNotProvided()
    {
        // Arrange
        var testObject = new TestModel { Id = 1, Name = "Test" };
        var jsonContent = JsonConvert.SerializeObject(testObject);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent),
        };

        // Act
        var result = await response.GetContent<TestModel>(_loggerMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetContent_HandlesComplexObjects()
    {
        // Arrange
        var complexObject = new ComplexTestModel
        {
            Id = 1,
            Name = "Complex",
            NestedObject = new TestModel { Id = 2, Name = "Nested" },
            Items = ["item1", "item2", "item3"],
        };
        var jsonContent = JsonConvert.SerializeObject(complexObject);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent),
        };

        // Act
        var result = await response.GetContent<ComplexTestModel>(_loggerMock.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(complexObject.Id, result.Id);
        Assert.Equal(complexObject.Name, result.Name);
        Assert.NotNull(result.NestedObject);
        Assert.Equal(complexObject.NestedObject.Id, result.NestedObject.Id);
        Assert.Equal(complexObject.Items.Count, result.Items.Count);
    }

    #endregion

    #region Test Models

    private class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ComplexTestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TestModel? NestedObject { get; set; }
        public List<string> Items { get; set; } = [];
    }

    #endregion
}
