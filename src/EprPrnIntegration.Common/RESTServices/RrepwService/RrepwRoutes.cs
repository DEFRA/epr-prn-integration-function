using System.Globalization;
using System.Web;

namespace EprPrnIntegration.Common.RESTServices.RrepwService
{
    public static class RrepwRoutes
    {
        private const string PackagingRecyclingNotesEndpoint = "packaging-recycling-notes";

        public static string ToUniversalDate(this DateTime date)
        {
            return date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }

        public static string ListPrnsRoute(
            List<string> statuses,
            string dateFrom,
            string dateTo,
            string? cursor
        )
        {
            var route =
                $"{PackagingRecyclingNotesEndpoint}?statuses={string.Join(",", statuses)}&dateFrom={dateFrom}&dateTo={dateTo}";

            if (!string.IsNullOrEmpty(cursor))
            {
                route += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            return route;
        }

        public static string AcceptPrnRoute(string prnNumber)
        {
            return $"{PackagingRecyclingNotesEndpoint}/{prnNumber}/accept";
        }

        public static string RejectPrnRoute(string prnNumber)
        {
            return $"{PackagingRecyclingNotesEndpoint}/{prnNumber}/reject";
        }
    }
}
