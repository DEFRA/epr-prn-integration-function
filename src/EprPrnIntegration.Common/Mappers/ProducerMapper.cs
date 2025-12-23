using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Mappers
{
    public static class ProducerMapper
    {
        public static ProducerDelta Map(
            List<UpdatedProducersResponse> updatedEprProducers,
            IConfiguration configuration
        )
        {
            var entityTypeNames = new Dictionary<string, string>
            {
                { "CSM", "Scheme Member" },
                { "SUB", "SUBSIDIARY" },
                { "DR", "Direct Registrant" },
                { "CS", "Compliance Scheme" },
            };

            var statusMapping = new Dictionary<string, string>
            {
                { "PR-CLOSED", "Closed" },
                { "PR-REGISTERED", "Registered" },
                { "PR-CANCELLED", "Cancelled" },
                { "PR-NOTREGISTERED", "Not Registered" },
                { "CSR-REGISTERED", "Registered" },
                { "CSR-CANCELLED", "Cancelled" },
            };

            var producersContext = configuration["ProducersContext"] ?? string.Empty;
            if (updatedEprProducers == null || updatedEprProducers.Count.Equals(0))
            {
                return new ProducerDelta { Context = producersContext, Value = [] };
            }

            return new ProducerDelta
            {
                Context = producersContext,
                Value = updatedEprProducers
                    .Select(eprProducer =>
                    {
                        var codes = GetCodes(eprProducer.Status, eprProducer.OrganisationType);
                        var entityTypeCode = codes.EntityTypeCode;
                        var statusCode = codes.StatusCode;

                        return new Producer
                        {
                            AddressLine1 = eprProducer.AddressLine1 ?? string.Empty,
                            AddressLine2 = eprProducer.AddressLine2 ?? string.Empty,
                            CompanyRegNo = eprProducer.CompaniesHouseNumber ?? string.Empty,
                            Country = eprProducer.Country ?? string.Empty,
                            County = eprProducer.County ?? string.Empty,
                            Town = eprProducer.Town ?? string.Empty,
                            Postcode = eprProducer.Postcode ?? string.Empty,
                            EntityTypeCode = entityTypeCode,
                            EntityTypeName = string.IsNullOrEmpty(entityTypeCode)
                                ? string.Empty
                                : entityTypeNames.GetValueOrDefault(entityTypeCode, string.Empty),
                            StatusCode = statusCode,
                            StatusDesc = string.IsNullOrEmpty(statusCode)
                                ? string.Empty
                                : statusMapping.GetValueOrDefault(statusCode, string.Empty),
                            EPRId = eprProducer.PEPRID ?? string.Empty,
                            EPRCode = eprProducer.OrganisationId ?? string.Empty,
                            ProducerName = eprProducer.OrganisationName ?? string.Empty,
                            Agency = GetAgencyByCountry(
                                eprProducer.BusinessCountry ?? string.Empty
                            ),
                            TradingName = eprProducer.TradingName ?? string.Empty,
                        };
                    })
                    .ToList(),
            };
        }

        public static string MapAddress(Producer producer)
        {
            if (producer == null)
                return string.Empty;

            var addressFields = new[]
            {
                producer.AddressLine1,
                producer.AddressLine2,
                producer.Town,
                producer.County,
                producer.Postcode,
            };

            return string.Join(", ", addressFields.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static (string StatusCode, string EntityTypeCode) GetCodes(
            string? status,
            string? orgType
        )
        {
            if (string.IsNullOrEmpty(status) || string.IsNullOrEmpty(orgType))
            {
                return (string.Empty, string.Empty);
            }

            // DR Registered
            if (status == "DR Registered" && orgType == "DR")
            {
                return ("PR-REGISTERED", "DR");
            }

            // DR Deleted or CSO Deleted
            if ((status == "DR Deleted" || status == "CSO Deleted") && orgType == "DR")
            {
                return ("PR-CANCELLED", "DR");
            }

            // DR Moved to CS
            if (status == "DR Moved to CS" && orgType == "CSM")
            {
                return ("PR-REGISTERED", "CSM");
            }

            // Not a Member of CS
            if (status == "Not a Member of CS" && orgType == "DR")
            {
                return ("PR-REGISTERED", "DR");
            }

            // CS Added
            if (status == "CS Added" && orgType == "S")
            {
                return ("CSR-REGISTERED", "CS");
            }

            // CS Deleted
            if (status == "CS Deleted" && orgType == "S")
            {
                return ("CSR-CANCELLED", "CS");
            }

            return (string.Empty, string.Empty);
        }

        private static string GetAgencyByCountry(string businessCountry)
        {
            switch (businessCountry)
            {
                case "England":
                    return "Environment Agency";
                case "Northern Ireland":
                    return "Northern Ireland Environment Agency";
                case "Wales":
                    return "Natural Resources Wales";
                case "Scotland":
                    return "Scottish Environment Protection Agency";
                default:
                    return string.Empty;
            }
        }
    }
}
