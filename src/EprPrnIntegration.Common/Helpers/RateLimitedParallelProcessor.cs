using System.Threading.RateLimiting;

namespace EprPrnIntegration.Common.Helpers;

public static class RateLimitedParallelProcessor
{
    /// <summary>
    /// Processes items in parallel with rate limiting applied.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The collection of items to process.</param>
    /// <param name="processor">The async function to apply to each item.</param>
    /// <param name="requestsPerSecond">The maximum number of requests per second.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task ProcessAsync<T>(
        IEnumerable<T> items,
        Func<T, Task> processor,
        int requestsPerSecond,
        CancellationToken cancellationToken = default
    )
    {
        using var rateLimiter = new TokenBucketRateLimiter(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = requestsPerSecond,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = int.MaxValue,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = requestsPerSecond,
                AutoReplenishment = true,
            }
        );

        await Parallel.ForEachAsync(
            items,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = requestsPerSecond,
                CancellationToken = cancellationToken,
            },
            async (item, ct) =>
            {
                using var lease = await rateLimiter.AcquireAsync(permitCount: 1, ct);
                await processor(item);
            }
        );
    }
}
