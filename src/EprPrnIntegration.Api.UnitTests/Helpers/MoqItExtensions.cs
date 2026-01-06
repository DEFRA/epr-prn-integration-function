using Moq;

namespace EprPrnIntegration.Api.UnitTests.Helpers;

public class ItEx
{
    public static DateTime IsCloseTo(DateTime date)
    {
        return It.Is<DateTime>(d => Math.Abs((date - d).TotalMilliseconds) < 10000);
    }
}
