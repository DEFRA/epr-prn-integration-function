using EprPrnIntegration.Common.RESTServices.RrepwService;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.RrepwService
{
    public class RrepwRoutesTests
    {
        [Fact]
        public void ListPrnsRouteReturnsCorrectUrl()
        {
            var statuses = new List<string> { "awaiting-acceptance", "cancelled" };
            var dateFrom = "2023-01-01T00:00:00Z";
            var dateTo = "2023-12-31T23:59:59Z";
            var cursor = "cursor-value";

            RrepwRoutes
                .ListPrnsRoute(statuses, dateFrom, dateTo, cursor)
                .Should()
                .Be(
                    "v1/packaging-recycling-notes?statuses=awaiting-acceptance,cancelled&dateFrom=2023-01-01T00:00:00Z&dateTo=2023-12-31T23:59:59Z&cursor=cursor-value"
                );
        }

        [Fact]
        public void ListPrnsRouteReturnsCorrectUrl_NoCursor()
        {
            var statuses = new List<string> { "awaiting-acceptance", "cancelled" };
            var dateFrom = "2023-01-01T00:00:00Z";
            var dateTo = "2023-12-31T23:59:59Z";

            RrepwRoutes
                .ListPrnsRoute(statuses, dateFrom, dateTo, null)
                .Should()
                .Be(
                    "v1/packaging-recycling-notes?statuses=awaiting-acceptance,cancelled&dateFrom=2023-01-01T00:00:00Z&dateTo=2023-12-31T23:59:59Z"
                );
        }

        [Fact]
        public void AcceptPrnRouteReturnsCorrectUrl()
        {
            RrepwRoutes
                .AcceptPrnRoute("PRN12345")
                .Should()
                .Be("v1/packaging-recycling-notes/PRN12345/accept");
        }

        [Fact]
        public void RejectPrnRouteReturnsCorrectUrl()
        {
            RrepwRoutes
                .RejectPrnRoute("PRN12345")
                .Should()
                .Be("v1/packaging-recycling-notes/PRN12345/reject");
        }

        [Fact]
        public void ToUniversalDateReturnsCorrectFormat()
        {
            RrepwRoutes
                .ToUniversalDate(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local))
                .Should()
                .Be("2023-01-01T12:00:00.0000000Z");
        }
    }
}
