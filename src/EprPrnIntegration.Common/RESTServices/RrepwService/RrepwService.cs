using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage(Justification = "This will have test coverage via integration tests.")]
    public class RrepwService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<RrepwService> logger,
        IOptions<RrepwApiConfiguration> config
    )
        : BaseHttpService(
            httpContextAccessor,
            httpClientFactory,
            config.Value.BaseUrl
                ?? throw new ArgumentNullException(
                    nameof(config),
                    ExceptionMessages.RrepwApiBaseUrlMissing
                ),
            "v1",
            logger,
            HttpClientNames.Rrepw,
            config.Value.TimeoutSeconds
        ),
            IRrepwService
    {
        public async Task<List<PackagingRecyclingNote>> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default
        )
        {
            var dateFromQuery = dateFrom
                .ToUniversalTime()
                .ToString("O", CultureInfo.InvariantCulture);
            var dateToQuery = dateTo.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            var statuses = string.Join(",", RrepwStatus.AwaitingAcceptance, RrepwStatus.Cancelled);

            var allItems = new List<PackagingRecyclingNote>();
            string? cursor = null;
            var pageCount = 0;

            do
            {
                pageCount++;
                var (items, nextCursor) = await FetchPage(
                    statuses,
                    dateFromQuery,
                    dateToQuery,
                    cursor,
                    pageCount,
                    cancellationToken
                );

                if (items.Count > 0)
                {
                    allItems.AddRange(items);
                }

                cursor = nextCursor;
            } while (!string.IsNullOrEmpty(cursor));

            logger.LogInformation(
                "Completed fetching packaging recycling notes. Total pages: {PageCount}, Total items: {TotalCount}",
                pageCount,
                allItems.Count
            );

            return allItems;
        }

        private async Task<(List<PackagingRecyclingNote> Items, string? NextCursor)> FetchPage(
            string statuses,
            string dateFromQuery,
            string dateToQuery,
            string? cursor,
            int pageCount,
            CancellationToken cancellationToken
        )
        {
            var url = BuildRoute(statuses, dateFromQuery, dateToQuery, cursor);

            logger.LogInformation(
                "Fetching packaging recycling notes from {DateFrom} to {DateTo}, page {PageCount}",
                dateFromQuery,
                dateToQuery,
                pageCount
            );

            var response = await Get<ListPackagingRecyclingNotesResponse>(
                url,
                cancellationToken,
                includeTrailingSlash: false
            );

            var items = response.Items ?? new List<PackagingRecyclingNote>();

            if (items.Count > 0)
            {
                logger.LogInformation(
                    "Retrieved {ItemCount} items on page {PageCount}",
                    items.Count,
                    pageCount
                );
            }

            var nextCursor = response.HasMore ? response.NextCursor : null;

            return (items, nextCursor);
        }

        private static string BuildRoute(
            string statuses,
            string dateFrom,
            string dateTo,
            string? cursor
        )
        {
            var route =
                $"packaging-recycling-notes?statuses={statuses}&dateFrom={dateFrom}&dateTo={dateTo}";

            if (!string.IsNullOrEmpty(cursor))
            {
                route += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            return route;
        }
    }
}
