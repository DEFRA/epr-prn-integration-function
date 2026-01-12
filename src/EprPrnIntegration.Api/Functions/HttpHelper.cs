using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Helpers;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

public static class HttpHelper
{
    public static async Task<bool> HandleTransientErrors(
        Func<Task<HttpResponseMessage>> action,
        ILogger logger,
        string message
    )
    {
        HttpResponseMessage response;
        try
        {
            response = await action();

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation($"{message} - success");
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{message} - exception, continuing with next");
            return false;
        }

        // Transient errors after Polly retries exhausted - terminate function to retry on next schedule
        if (response.StatusCode.IsTransient())
        {
            logger.LogError(
                $"{message} - transient error {{StatusCode}}, terminating",
                response.StatusCode
            );
            throw new ServiceException(
                $"{message} - transient error {response.StatusCode}, terminating",
                response.StatusCode
            );
        }

        // Non-transient errors are not recoverable; log and continue with next PRN
        logger.LogError(
            $"{message} - non transient error {{StatusCode}}, continuing with next",
            response.StatusCode
        );
        return false;
    }
}
