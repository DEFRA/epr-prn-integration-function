namespace EprPrnIntegration.Common.RESTServices.PrnBackendService
{
    public static class PrnRoutes
    {
        public static string ModifiedPrnsRoute(DateTime dateFrom, DateTime dateTo)
        {
            var fromDate = dateFrom.ToString(
                "yyyy-MM-ddTHH:mm:ss.fff",
                System.Globalization.CultureInfo.InvariantCulture
            );
            var toDate = dateTo.ToString(
                "yyyy-MM-ddTHH:mm:ss.fff",
                System.Globalization.CultureInfo.InvariantCulture
            );
            return $"api/v2/prn/modified-prns?dateFrom={fromDate}&dateTo={toDate}";
        }
    }
}
