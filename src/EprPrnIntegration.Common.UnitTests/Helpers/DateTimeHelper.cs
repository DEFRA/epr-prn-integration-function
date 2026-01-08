using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace EprPrnIntegration.Common.Helpers;

[ExcludeFromCodeCoverage]
public static class DateTimeHelper
{
    public static DateTime NewUtcDateTime(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime Parse(string dateTime)
    {
        // e.g. "2024-10-10"
        return DateTime.Parse(dateTime, CultureInfo.InvariantCulture);
    }
}
