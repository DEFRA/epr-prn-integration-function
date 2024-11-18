using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;

namespace EprPrnIntegration.Common.Mappers;

public static class ProducerMapper
{
    public static List<Producer> Map(List<UpdatedProducersResponseModel> updatedEprProducers)
    {
        if (updatedEprProducers == null || !updatedEprProducers.Any())
        {
            return new List<Producer>();
        }

        return updatedEprProducers.Select(eprProducer => new Producer
        {
            AddressLine1 = $"{eprProducer.SubBuildingName} {eprProducer.BuildingNumber} {eprProducer.BuildingName}".Trim(),
            AddressLine2 = eprProducer.Street,
            AddressLine3 = eprProducer.Locality,
            AddressLine4 = eprProducer.DependentLocality,
            Town = eprProducer.Town,
            County = eprProducer.County,
            Country = eprProducer.Country,
            ProducerName = eprProducer.ProducerName,
            CompanyRegNo = eprProducer.CompaniesHouseNumber,
            Postcode = eprProducer.Postcode,
            EntityTypeCode = eprProducer.ProducerTypeId?.ToString(),
            NPWDCode = eprProducer.ReferenceNumber,
            EPRId = eprProducer.OrganisationId.ToString(),
            EPRCode = eprProducer.OrganisationTypeId.ToString(),
            StatusCode = eprProducer.StatusCode,
            StatusDesc = eprProducer.StatusDesc,
            StatusDate = eprProducer.StatusDate
        }).ToList();
    }
}