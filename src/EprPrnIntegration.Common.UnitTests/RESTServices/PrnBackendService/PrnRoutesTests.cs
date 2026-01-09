using EprPrnIntegration.Common.RESTServices.PrnBackendService;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.RESTServices.PrnBackendService
{
    public class PrnRoutesTests
    {
        [Fact]
        public void ModifiedPrnsRoute_ReturnsCorrectUrl()
        {
            var dateFrom = new DateTime(2023, 1, 15, 10, 30, 45, 123, DateTimeKind.Utc);
            var dateTo = new DateTime(2023, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);

            PrnRoutes
                .ModifiedPrnsRoute(dateFrom, dateTo)
                .Should()
                .Be(
                    "api/v2/prn/modified-prns?from=2023-01-15T10:30:45.123&to=2023-12-31T23:59:59.999"
                );
        }

        [Fact]
        public void ModifiedPrnsRoute_FormatsDateWithMilliseconds()
        {
            var dateFrom = new DateTime(2024, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var dateTo = new DateTime(2024, 6, 30, 12, 0, 0, 500, DateTimeKind.Utc);

            var result = PrnRoutes.ModifiedPrnsRoute(dateFrom, dateTo);

            result.Should().Contain("from=2024-06-01T00:00:00.000");
            result.Should().Contain("to=2024-06-30T12:00:00.500");
        }

        [Fact]
        public void ModifiedPrnsRoute_StartsWithCorrectPath()
        {
            var dateFrom = DateTime.UtcNow.AddDays(-1);
            var dateTo = DateTime.UtcNow;

            var result = PrnRoutes.ModifiedPrnsRoute(dateFrom, dateTo);

            result.Should().StartWith("api/v2/prn/modified-prns?");
        }
    }
}
