using EprPrnIntegration.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Models.Npwd;

namespace EprPrnIntegration.Common.Mappers;

public static class PrnMapper
{
    public static PrnDelta Map(
        List<UpdatedPrnsResponseModel> updatedPrns,
        IConfiguration configuration)
    {
        var prnsContext = configuration["PrnsContext"] ?? string.Empty;
        
        // Resolve configured default obligation year safely
        var obligationYear = ObligationYearResolver.GetDefaultObligationYear(
            configuration,
            NullLogger.Instance
        );
        
        if (updatedPrns == null || updatedPrns.Count.Equals(0))
        {
            return new PrnDelta
            {
                Context = prnsContext,
                Value = []
            };
        }

        return new PrnDelta
        {
            Context = prnsContext,
            Value = updatedPrns.Select(eprProducer => new UpdatedPrnsResponseModel
            {
                EvidenceNo = eprProducer.EvidenceNo,
                EvidenceStatusCode = eprProducer.EvidenceStatusCode,
                StatusDate = eprProducer.StatusDate,
                ObligationYear = obligationYear
            }).ToList()
        };
    }
}