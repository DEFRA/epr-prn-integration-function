using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Mappers;

public static class ProducerMapper
{
    public static ProducerDelta Map(
        List<UpdatedProducersResponseModel> updatedEprProducers, IConfiguration configuration)
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
                AddressLine1 = $"{eprProducer.SubBuildingName} {eprProducer.BuildingNumber} {eprProducer.BuildingName}",
                AddressLine2 = eprProducer.Street,
                CompanyRegNo = eprProducer.CompaniesHouseNumber,
                EntityTypeCode = eprProducer.IsComplianceScheme ? "CS" : "DR",
                EntityTypeName = eprProducer.IsComplianceScheme ? "Compliance Scheme" : "Direct Registrant",
                EPRId = eprProducer.ExternalId,
                EPRCode = eprProducer.ReferenceNumber,
                Postcode = eprProducer.Postcode,
                ProducerName = eprProducer.ProducerName
            }).ToList()
        };
    }
}