using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Mappers;

public static class PrnMapper
{
    public static PrnDelta Map(
        List<UpdatedPrnsResponseModel> updatedPrns, IConfiguration configuration)
    {
        if (updatedPrns == null || !updatedPrns.Any())
        {
            return new PrnDelta { Context = configuration["PrnsContext"], Value = [] };
        }

        return new PrnDelta
        {
            Context = configuration["PrnsContext"],
            Value = updatedPrns.Select(eprProducer => new UpdatedPrnsResponseModel
            {
                EvidenceNo = eprProducer.EvidenceNo,
                EvidenceStatusCode = eprProducer.EvidenceStatusCode,
                StatusDate = eprProducer.StatusDate
            }).ToList()
        };
    }
}