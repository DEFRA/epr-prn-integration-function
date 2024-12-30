using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Mappers;

public static class ProducerMapper
{
    public static ProducerDelta Map(
        List<UpdatedProducersResponse> updatedEprProducers, IConfiguration configuration)
    {
        if (updatedEprProducers == null || !updatedEprProducers.Any())
        {
            return new ProducerDelta { Context = configuration["ProducersContext"], Value = [] };
        }

        return new ProducerDelta
        {
            Context = configuration["ProducersContext"],
            Value = updatedEprProducers.Select(eprProducer => new Producer
            {
                AddressLine1 = eprProducer.AddressLine1 ?? string.Empty,
                AddressLine2 = eprProducer.AddressLine2 ?? string.Empty,
                CompanyRegNo = eprProducer.CompaniesHouseNumber ?? string.Empty,
                //TODO: logic for the below
                //EntityTypeCode = eprProducer.IsComplianceScheme ? "CS" : "DR", // need
                //EntityTypeName = eprProducer.IsComplianceScheme ? "Compliance Scheme" : "Direct Registrant", // needed

                Country = eprProducer.Country ?? string.Empty,
                County = eprProducer.County ?? string.Empty,
                Town = eprProducer.Town ?? string.Empty,
                Postcode = eprProducer.Postcode ?? string.Empty,
                //TODO add the statusCode & statusDesc fields once available through the stored procedure
                //statusCode & statusDesc

                //Do we need these? If yes, how to add these available via the stored procedure
                //EPRId = eprProducer.ExternalId,
                //EPRCode = eprProducer.ReferenceNumber,
                //ProducerName = eprProducer.ProducerName

            }).ToList()
        };
    }
}