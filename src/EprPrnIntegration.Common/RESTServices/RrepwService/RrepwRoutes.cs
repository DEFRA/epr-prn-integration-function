using System.Globalization;

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
                $"v1/{PackagingRecyclingNotesEndpoint}?statuses={string.Join(",", statuses)}&dateFrom={dateFrom}&dateTo={dateTo}";

            if (!string.IsNullOrEmpty(cursor))
            {
                route += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            return route;
        }

        public static string AcceptPrnRoute(string prnNumber)
        {
            return $"v1/{PackagingRecyclingNotesEndpoint}/{prnNumber}/accept";
        }

        public static string RejectPrnRoute(string prnNumber)
        {
            return $"v1/{PackagingRecyclingNotesEndpoint}/{prnNumber}/reject";
        }
    }
}
