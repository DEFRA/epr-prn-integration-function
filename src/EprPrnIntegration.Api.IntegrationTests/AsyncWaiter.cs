using System.Diagnostics;

namespace EprPrnIntegration.Api.IntegrationTests;

public static class AsyncWaiter
{
    private static readonly TimeSpan s_defaultDelay = TimeSpan.FromSeconds(1);

    public static async Task WaitForAsync(Func<Task> assertion, double? timeout = null, TimeSpan? delay = null)
    {
        var timer = Stopwatch.StartNew();
        var timeoutTimespan = TimeSpan.FromSeconds(timeout ?? 30);

        while (true)
        {
            try
            {
                await assertion();

                break;
            }
            catch (Exception)
            {
                if (timer.Elapsed > timeoutTimespan)
                    throw;

                await Task.Delay(delay ?? s_defaultDelay);
            }
        }
    }
}
