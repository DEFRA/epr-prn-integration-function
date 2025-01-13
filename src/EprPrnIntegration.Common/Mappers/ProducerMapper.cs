using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using Microsoft.Extensions.Configuration;

namespace EprPrnIntegration.Common.Mappers
{
    public static class ProducerMapper
    {
        public static ProducerDelta Map(
            List<UpdatedProducersResponse> updatedEprProducers, IConfiguration configuration)
        {
            var entityTypeNames = new Dictionary<string, string>
            {
                { "CSM", "Scheme Member" },
                { "SUB", "SUBSIDIARY" },
                { "DR", "Direct Registrant" },
                { "CS", "Compliance Scheme" }
            };

            var statusMapping = new Dictionary<string, string>
            {
                { "PR-CLOSED", "Closed" },
                { "PR-REGISTERED", "Registered" },
                { "PR-CANCELLED", "Cancelled" },
                { "PR-NOTREGISTERED", "Not Registered" }
            };

            if (updatedEprProducers == null || !updatedEprProducers.Any())
            {
                return new ProducerDelta { Context = configuration["ProducersContext"], Value = new List<Producer>() };
            }

            return new ProducerDelta
            {
                Context = configuration["ProducersContext"],
                Value = updatedEprProducers.Select(eprProducer =>
                {
                    var entityTypeCode = GetEntityTypeCode(eprProducer);
                    var statusCode = GetStatusCode(eprProducer);

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
                        EntityTypeName = string.IsNullOrEmpty(entityTypeCode) ? string.Empty : entityTypeNames.GetValueOrDefault(entityTypeCode, string.Empty),
                        StatusCode = statusCode,
                        StatusDesc = string.IsNullOrEmpty(statusCode) ? string.Empty : statusMapping.GetValueOrDefault(statusCode, string.Empty),
                        EPRId = eprProducer.PEPRID ?? string.Empty,
                        EPRCode = eprProducer.OrganisationId ?? string.Empty,
                        ProducerName = eprProducer.OrganisationName ?? string.Empty,
                    };
                }).ToList()
            };
        }

        private static string GetStatusCode(UpdatedProducersResponse eprProducer)
        {
            return GetCode(eprProducer, isStatusCode: true);
        }

        private static string GetEntityTypeCode(UpdatedProducersResponse eprProducer)
        {
            return GetCode(eprProducer, isStatusCode: false);
        }

        private static string GetCode(UpdatedProducersResponse eprProducer, bool isStatusCode)
        {
            // DR Registered
            if (eprProducer.Status == "DR Registered" && eprProducer.OrganisationType == "DR")
            {
                return isStatusCode ? "PR-REGISTERED" : "DR";
            }

            // DR Deleted or CSO Deleted
            else if ((eprProducer.Status == "DR Deleted" || eprProducer.Status == "CSO Deleted") && eprProducer.OrganisationType == "DR")
            {
                return isStatusCode ? "PR-CANCELLED" : "DR";
            }

            // DR Moved to CS
            else if (eprProducer.Status == "DR Moved to CS" && eprProducer.OrganisationType == "CSM")
            {
                return isStatusCode ? "PR-REGISTERED" : "CSM";
            }

            // Not a Member of CS
            else if (eprProducer.Status == "Not a Member of CS" && eprProducer.OrganisationType == "DR")
            {
                return isStatusCode ? "PR-REGISTERED" : "DR";
            }

            // CS Added
            else if (eprProducer.Status == "CS Added" && eprProducer.OrganisationType == "S")
            {
                return isStatusCode ? "PR-REGISTERED" : "CS";
            }

            // CS Deleted
            else if (eprProducer.Status == "CS Deleted" && eprProducer.OrganisationType == "S")
            {
                return isStatusCode ? "PR-CANCELLED" : "CS";
            }

            return string.Empty;
        }
    }
}
