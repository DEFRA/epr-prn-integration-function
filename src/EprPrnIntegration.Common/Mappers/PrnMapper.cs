using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Mappers;

public static class PrnMapper
{
    public static PrnDelta Map(
        List<UpdatedPrnsResponseModel> updatedPrns, IConfiguration configuration)
    {
        string prnsContext = configuration["PrnsContext"] ?? string.Empty;
        if (updatedPrns == null || updatedPrns.Count.Equals(0))
        {
            return new PrnDelta { Context = prnsContext, Value = [] };
        }

        return new PrnDelta
        {
            Context = prnsContext,
            Value = updatedPrns.Select(eprProducer => new UpdatedPrnsResponseModel
            {
                EvidenceNo = eprProducer.EvidenceNo,
                EvidenceStatusCode = eprProducer.EvidenceStatusCode,
                StatusDate = eprProducer.StatusDate
            }).ToList()
        };
    }
}