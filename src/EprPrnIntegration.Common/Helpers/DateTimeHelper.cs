using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class DateTimeHelper
    {
        public static DateTime NewUtcDateTime(int year, int month, int day)
        {
            return new DateTime(year, month, day, 0 ,0 ,0 ,DateTimeKind.Utc);  
        }
    }
}
