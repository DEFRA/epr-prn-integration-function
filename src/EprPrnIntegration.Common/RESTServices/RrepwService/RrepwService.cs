using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
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
            logger,
            HttpClientNames.Rrepw,
            config.Value.TimeoutSeconds
        ),
            IRrepwService
    {
        public async Task<List<PackagingRecyclingNote>> ListPackagingRecyclingNotes(
            DateTime dateFrom,
            DateTime dateTo
        )
        {
            var dateFromQuery = dateFrom.ToUniversalDate();
            var dateToQuery = dateTo.ToUniversalDate();
            var statuses = new List<string>
            {
                RrepwStatus.AwaitingAcceptance,
                RrepwStatus.Cancelled,
            };

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
                    pageCount
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
            List<string> statuses,
            string dateFromQuery,
            string dateToQuery,
            string? cursor,
            int pageCount
        )
        {
            var url = RrepwRoutes.ListPrnsRoute(statuses, dateFromQuery, dateToQuery, cursor);

            logger.LogInformation(
                "Fetching packaging recycling notes from {DateFrom} to {DateTo}, page {PageCount}",
                dateFromQuery,
                dateToQuery,
                pageCount
            );

            var httpResponse = await GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();

            var response =
                await httpResponse.Content.ReadFromJsonAsync<ListPackagingRecyclingNotesResponse>();
            var items = response?.Items ?? [];

            if (items.Count > 0)
            {
                logger.LogInformation(
                    "Retrieved {ItemCount} items on page {PageCount}",
                    items.Count,
                    pageCount
                );
            }

            var nextCursor = response?.HasMore == true ? response.NextCursor : null;

            return (items, nextCursor);
        }

        public async Task<HttpResponseMessage> UpdatePrn(PrnUpdateStatus prn)
        {
            if (string.IsNullOrWhiteSpace(prn.SourceSystemId))
            {
                logger.LogWarning(
                    "Skipping PRN update due to missing SourceSystemId {PrnNumber}.",
                    prn.PrnNumber
                );
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            if (prn.PrnStatusId == (int)EprnStatus.ACCEPTED)
            {
                logger.LogInformation(
                    "Accepting PRN {PrnNumber} and SourceSystemId {SourceSystemId} with acceptedAt date {AcceptedAt}",
                    prn.PrnNumber,
                    prn.SourceSystemId,
                    prn.StatusDate
                );
                return await PostAsync(
                    RrepwRoutes.AcceptPrnRoute(prn.SourceSystemId),
                    new { acceptedAt = prn.StatusDate }
                );
            }
            else if (prn.PrnStatusId == (int)EprnStatus.REJECTED)
            {
                logger.LogInformation(
                    "Rejecting PRN {PrnNumber} and SourceSystemId {SourceSystemId} with rejectedAt date {RejectedAt}",
                    prn.PrnNumber,
                    prn.SourceSystemId,
                    prn.StatusDate
                );
                return await PostAsync(
                    RrepwRoutes.RejectPrnRoute(prn.SourceSystemId),
                    new { rejectedAt = prn.StatusDate }
                );
            }
            else
            {
                logger.LogWarning(
                    "Incorrect PRN status {PrnStatusId} for PRN {PrnNumber}; skipping.",
                    prn.PrnStatusId,
                    prn.PrnNumber
                );
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }
    }
}
