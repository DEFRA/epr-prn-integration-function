using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.RrepwService;

public class RrepwServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<Common.RESTServices.RrepwService.RrepwService>> _loggerMock;
    private readonly Mock<IOptions<RrepwApiConfiguration>> _mockConfig;

    public RrepwServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<Common.RESTServices.RrepwService.RrepwService>>();
        _mockConfig = new Mock<IOptions<RrepwApiConfiguration>>();

        _mockConfig.Setup(c => c.Value).Returns(new RrepwApiConfiguration
        {
            BaseUrl = "http://localhost:5001",
            TimeoutSeconds = 30
        });
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_SinglePage_ReturnsAllItems()
    {
        // Arrange
        var expectedItems = new List<PackagingRecyclingNote>
        {
            new() { Id = "1", PrnNumber = "PRN001", TonnageValue = 100 },
            new() { Id = "2", PrnNumber = "PRN002", TonnageValue = 200 }
        };

        var response = new ListPackagingRecyclingNotesResponse
        {
            Items = expectedItems,
            HasMore = false,
            NextCursor = null
        };

        var service = CreateRrepwService(JsonSerializer.Serialize(response));
        var dateFrom = new DateTime(2024, 1, 1);
        var dateTo = new DateTime(2024, 1, 31);

        // Act
        var result = await service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_MultiplePagesWithCursor_ReturnsAllItemsConcatenated()
    {
        // Arrange
        var page1Items = new List<PackagingRecyclingNote>
        {
            new() { Id = "1", PrnNumber = "PRN001", TonnageValue = 100 },
            new() { Id = "2", PrnNumber = "PRN002", TonnageValue = 200 }
        };

        var page2Items = new List<PackagingRecyclingNote>
        {
            new() { Id = "3", PrnNumber = "PRN003", TonnageValue = 300 },
            new() { Id = "4", PrnNumber = "PRN004", TonnageValue = 400 }
        };

        var page3Items = new List<PackagingRecyclingNote>
        {
            new() { Id = "5", PrnNumber = "PRN005", TonnageValue = 500 }
        };

        var page1Response = new ListPackagingRecyclingNotesResponse
        {
            Items = page1Items,
            HasMore = true,
            NextCursor = "cursor-page-2"
        };

        var page2Response = new ListPackagingRecyclingNotesResponse
        {
            Items = page2Items,
            HasMore = true,
            NextCursor = "cursor-page-3"
        };

        var page3Response = new ListPackagingRecyclingNotesResponse
        {
            Items = page3Items,
            HasMore = false,
            NextCursor = null
        };

        var mockHandler = new PaginatedMockHttpMessageHandler(new Dictionary<string, string>
        {
            { "page1", JsonSerializer.Serialize(page1Response) },
            { "cursor-page-2", JsonSerializer.Serialize(page2Response) },
            { "cursor-page-3", JsonSerializer.Serialize(page3Response) }
        });

        var httpClient = new HttpClient(mockHandler);
        var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

        var service = new Common.RESTServices.RrepwService.RrepwService(
            _mockHttpContextAccessor.Object,
            httpClientFactoryMock,
            _loggerMock.Object,
            _mockConfig.Object
        );

        var dateFrom = new DateTime(2024, 1, 1);
        var dateTo = new DateTime(2024, 1, 31);

        // Act
        var result = await service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result[0].Id.Should().Be("1");
        result[1].Id.Should().Be("2");
        result[2].Id.Should().Be("3");
        result[3].Id.Should().Be("4");
        result[4].Id.Should().Be("5");
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_EmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var response = new ListPackagingRecyclingNotesResponse
        {
            Items = new List<PackagingRecyclingNote>(),
            HasMore = false,
            NextCursor = null
        };

        var service = CreateRrepwService(JsonSerializer.Serialize(response));
        var dateFrom = new DateTime(2024, 1, 1);
        var dateTo = new DateTime(2024, 1, 31);

        // Act
        var result = await service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListPackagingRecyclingNotes_TenPages_ReturnsAllItemsConcatenated()
    {
        // Arrange
        var responses = new Dictionary<string, string>();
        var totalExpectedItems = 0;

        for (int i = 1; i <= 10; i++)
        {
            var pageItems = new List<PackagingRecyclingNote>
            {
                new() { Id = $"{i}-1", PrnNumber = $"PRN{i}01", TonnageValue = i * 100 },
                new() { Id = $"{i}-2", PrnNumber = $"PRN{i}02", TonnageValue = i * 100 + 50 }
            };

            totalExpectedItems += 2;

            var pageResponse = new ListPackagingRecyclingNotesResponse
            {
                Items = pageItems,
                HasMore = i < 10,
                NextCursor = i < 10 ? $"cursor-page-{i + 1}" : null
            };

            var key = i == 1 ? "page1" : $"cursor-page-{i}";
            responses[key] = JsonSerializer.Serialize(pageResponse);
        }

        var mockHandler = new PaginatedMockHttpMessageHandler(responses);
        var httpClient = new HttpClient(mockHandler);
        var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

        var service = new Common.RESTServices.RrepwService.RrepwService(
            _mockHttpContextAccessor.Object,
            httpClientFactoryMock,
            _loggerMock.Object,
            _mockConfig.Object
        );

        var dateFrom = new DateTime(2024, 1, 1);
        var dateTo = new DateTime(2024, 1, 31);

        // Act
        var result = await service.ListPackagingRecyclingNotes(dateFrom, dateTo);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(totalExpectedItems);
        result.First().Id.Should().Be("1-1");
        result.Last().Id.Should().Be("10-2");
    }

    private Common.RESTServices.RrepwService.RrepwService CreateRrepwService(
        string responseContent,
        System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        var mockHandler = new MockHttpMessageHandler(responseContent, statusCode);
        var httpClient = new HttpClient(mockHandler);
        var httpClientFactoryMock = new HttpClientFactoryMock(httpClient);

        return new Common.RESTServices.RrepwService.RrepwService(
            _mockHttpContextAccessor.Object,
            httpClientFactoryMock,
            _loggerMock.Object,
            _mockConfig.Object
        );
    }
}

// Helper class to handle paginated responses
public class PaginatedMockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses;
    private int _requestCount = 0;

    public PaginatedMockHttpMessageHandler(Dictionary<string, string> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requestCount++;

        string responseContent;

        // Extract cursor from query string
        var uri = request.RequestUri;
        var query = uri?.Query;

        if (string.IsNullOrEmpty(query) || !query.Contains("cursor="))
        {
            // First request - no cursor
            responseContent = _responses["page1"];
        }
        else
        {
            // Subsequent requests - extract cursor from query string
            // Extract cursor parameter
            var cursorMatch = System.Text.RegularExpressions.Regex.Match(query, @"cursor=([^&]+)");
            if (!cursorMatch.Success)
            {
                throw new InvalidOperationException("Cursor parameter not found in query string");
            }

            var cursor = System.Web.HttpUtility.UrlDecode(cursorMatch.Groups[1].Value);

            if (!_responses.TryGetValue(cursor, out responseContent!))
            {
                throw new InvalidOperationException($"No response configured for cursor: {cursor}");
            }
        }

        var httpResponse = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
        };

        return Task.FromResult(httpResponse);
    }
}
