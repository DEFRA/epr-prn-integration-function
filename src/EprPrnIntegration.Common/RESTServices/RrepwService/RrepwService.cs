using System.Globalization;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.RESTServices.RrepwService
{
    public class RrepwService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<RrepwService> logger,
        IOptions<RrepwApiConfiguration> config)
        : BaseHttpService(httpContextAccessor, httpClientFactory,
            config.Value.BaseUrl ??
            throw new ArgumentNullException(nameof(config), ExceptionMessages.RrepwApiBaseUrlMissing),
            "v1/packaging-recycling-notes",
            logger,
            HttpClientNames.Rrepw,
            config.Value.TimeoutSeconds), IRrepwService
    {
        public async Task<List<PackagingRecyclingNote>> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default)
        {
            var dateFromQuery = dateFrom.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            var dateToQuery = dateTo.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            var statuses = string.Join(",", RrepwStatus.AwaitingAcceptance, RrepwStatus.Cancelled);

            var allItems = new List<PackagingRecyclingNote>();
            string? cursor = null;
            var pageCount = 0;

            do
            {
                pageCount++;
                var queryString = BuildQueryString(statuses, dateFromQuery, dateToQuery, cursor);

                logger.LogInformation(
                    "Fetching packaging recycling notes from {DateFrom} to {DateTo}, page {PageCount}",
                    dateFromQuery, dateToQuery, pageCount);

                var response = await Get<ListPackagingRecyclingNotesResponse>(queryString, cancellationToken, includeTrailingSlash: false);

                if (response.Items != null && response.Items.Count > 0)
                {
                    allItems.AddRange(response.Items);
                    logger.LogInformation(
                        "Retrieved {ItemCount} items on page {PageCount}, total items so far: {TotalCount}",
                        response.Items.Count, pageCount, allItems.Count);
                }

                cursor = response.HasMore ? response.NextCursor : null;

            } while (!string.IsNullOrEmpty(cursor));

            logger.LogInformation(
                "Completed fetching packaging recycling notes. Total pages: {PageCount}, Total items: {TotalCount}",
                pageCount, allItems.Count);

            return allItems;
        }

        private static string BuildQueryString(string statuses, string dateFrom, string dateTo, string? cursor)
        {
            var queryString = $"?statuses={statuses}&dateFrom={dateFrom}&dateTo={dateTo}";

            if (!string.IsNullOrEmpty(cursor))
            {
                queryString += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            return queryString;
        }
    }
}
