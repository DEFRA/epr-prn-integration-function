using EprPrnIntegration.Common.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Common.Helpers
{
    public static class ObligationYearResolver
    {
        public static string GetDefaultObligationYear(IConfiguration config, ILogger logger)
        {
            var rawValue = config["DefaultObligationYear"];

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                logger.LogWarning("DefaultObligationYear missing or empty. Falling back to default.");
                return ObligationYearDefaults.ObligationYear2025;
            }

            if (int.TryParse(rawValue, out int parsed) && parsed is >= 1990 and <= 2100)
            {
                return parsed.ToString();
            }

            logger.LogWarning("DefaultObligationYear '{ConfigValue}' invalid. Falling back to default.", rawValue);
            return ObligationYearDefaults.ObligationYear2025;
        }
    }
}