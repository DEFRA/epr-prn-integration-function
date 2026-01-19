using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace EprPrnIntegration.Common.Mappers;

public static class PrnMapper
{
    public static PrnDelta Map(
        List<UpdatedNpwdPrnsResponseModel> updatedPrns,
        IConfiguration configuration
    )
    {
        var prnsContext = configuration["PrnsContext"] ?? string.Empty;

        // Resolve configured default obligation year safely
        var obligationYear = ObligationYearResolver.GetDefaultObligationYear(
            configuration,
            NullLogger.Instance
        );

        if (updatedPrns == null || updatedPrns.Count.Equals(0))
        {
            return new PrnDelta { Context = prnsContext, Value = [] };
        }

        return new PrnDelta
        {
            Context = prnsContext,
            Value = updatedPrns
                .Select(eprProducer => new UpdatedNpwdPrnsResponseModel
                {
                    EvidenceNo = eprProducer.EvidenceNo,
                    EvidenceStatusCode = eprProducer.EvidenceStatusCode,
                    StatusDate = eprProducer.StatusDate,
                    ObligationYear = eprProducer.ObligationYear ?? obligationYear,
                })
                .ToList(),
        };
    }
}
