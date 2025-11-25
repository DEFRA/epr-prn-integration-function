using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;

namespace EprPrnIntegration.Common.Mappers;

public static class ProducerUpdateRequestMapper
{
    public static ProducerUpdateRequest Map(UpdatedProducersResponse updatedProducer)
    {
        if (updatedProducer.PEPRID == null)
        {
            throw new ArgumentException("PEPRID is null");
        }
        
        if (updatedProducer.OrganisationName == null)
        {
            throw new ArgumentException("OrganisationName is null");
        }
        
        var mappedStatus = MapStatusEnums(updatedProducer.Status, updatedProducer.OrganisationType);
        
        return new ProducerUpdateRequest
        {
            Id = updatedProducer.PEPRID,
            Name = updatedProducer.OrganisationName,
            TradingName = updatedProducer.TradingName,
            AddressLine1 = updatedProducer.AddressLine1,
            AddressLine2 = updatedProducer.AddressLine2,
            Town = updatedProducer.Town,
            County = updatedProducer.County,
            Postcode = updatedProducer.Postcode,
            Country = updatedProducer.Country,
            
            Status = mappedStatus.Status,
            Type = mappedStatus.Type
        };
    }

    private static (ProducerStatus? Status, ProducerType? Type) MapStatusEnums(string? status, string? orgType)
    {
        return (status, orgType) switch
        {
            // Possible values for `status, orgType` taken from latest sproc code:
            // https://github.com/DEFRA/epr-data-sqldb/blob/main/dbo/Stored%20Procedures/sp_PRN_Delta_Extract.sql
            ("DR Registered", "DR") => (ProducerStatus.PrRegistered, ProducerType.DR),
            ("DR Deleted", "DR") => (ProducerStatus.PrCancelled, ProducerType.DR),
            ("CS Added", "S") => (ProducerStatus.CsrRegistered, ProducerType.CS),
            ("CS Deleted", "S") => (ProducerStatus.CsrCancelled, ProducerType.CS),
            _ => (null, null)
        };
    }
}