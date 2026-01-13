using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EprPrnIntegration.Api.Functions;

public static class HttpHelper
{
    public static async Task<bool> HandleTransientErrors(
        Func<CancellationToken, Task<HttpResponseMessage>> action,
        ILogger logger,
        string message,
        CancellationToken cancellationToken
    )
    {
        HttpResponseMessage response;
        try
        {
            response = await action(cancellationToken);

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

    public static async Task<T?> HandleTransientErrorsGet<T>(
        Func<CancellationToken, Task<HttpResponseMessage>> action,
        ILogger logger,
        string message,
        CancellationToken cancellationToken
    )
        where T : class
    {
        T? content = null;
        await HandleTransientErrors(
            async (ct) =>
            {
                var response = await action(ct);
                if (response.IsSuccessStatusCode)
                    content = await response.GetContent<T>(logger, ct);
                return response;
            },
            logger,
            message,
            cancellationToken
        );
        return content;
    }
}

public static class HttpResponseMessageExtensions
{
    public static async Task<T?> GetContent<T>(
        this HttpResponseMessage message,
        ILogger logger,
        CancellationToken? cancellationToken = null
    )
        where T : class
    {
        if (message.IsSuccessStatusCode)
        {
            var content = await message.Content.ReadAsStringAsync(
                cancellationToken ?? CancellationToken.None
            );

            if (string.IsNullOrWhiteSpace(content))
            {
                logger.LogError(
                    "Expected content in http response but got none {Type}",
                    typeof(T).FullName
                );
                return null;
            }

            var ret = JsonConvert.DeserializeObject<T>(content);
            if (ret is null)
            {
                logger.LogError(
                    "Failed to deserialize content for type {Type}",
                    typeof(T).FullName
                );
            }
            return ret;
        }
        else
        {
            logger.LogError(
                "Cannot get content for {Type} as status is not success",
                typeof(T).FullName
            );
            return null;
        }
    }
}
