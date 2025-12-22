using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Mappers
{
    public class WasteOrganisationsApiUpdateRequestMapperTests
    {
        private readonly Mock<ILogger<WasteOrganisationsApiUpdateRequestMapperTests>> _loggerMock = new();

        [Fact]
        public void MapsTopLevelFields()
        {
            var input = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                
                AddressLine1 = "address1",
                AddressLine2 = "address2",
                Town = "town",
                County = "county",
                Postcode = "postcode",
                Country = "UK",
                
                CompaniesHouseNumber = "some-company-number",
                OrganisationName = "Organisation's Name",
                TradingName = "Organisation's TradingName",

                RegistrationYear = "2025",
                Status = "registered",
                OrganisationType = "DP"
            };
            
            var result = WasteOrganisationsApiUpdateRequestMapper.Map(input, _loggerMock.Object);

            result.Name.Should().Be("Organisation's Name");
            result.TradingName.Should().Be("Organisation's TradingName");
            result.CompaniesHouseNumber.Should().Be("some-company-number");
        }

        [Fact]
        public void MapsAddress()
        {
            var input = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                
                AddressLine1 = "address1",
                AddressLine2 = "address2",
                Town = "town",
                County = "county",
                Postcode = "postcode",
                Country = "UK",
                

                Status = "registered",
                OrganisationType = "DP",
                RegistrationYear = "2025",

                CompaniesHouseNumber = "some-company-number",
                OrganisationName = "Organisation's Name",
                TradingName = "Organisation's TradingName"
            };
            
            var result = WasteOrganisationsApiUpdateRequestMapper.Map(input, _loggerMock.Object);

            result.Address.Should().BeEquivalentTo(new Address
            {
                AddressLine1 = "address1",
                AddressLine2 = "address2",
                Town = "town",
                County = "county",
                Postcode = "postcode",
                Country = "UK",
            });
        }

        [Theory]
        [InlineData("England", "GB-ENG")]
        [InlineData("Northern Ireland", "GB-NIR")]
        [InlineData("Wales", "GB-WLS")]
        [InlineData("Scotland", "GB-SCT")]
        [InlineData("", null)]
        public void MapsBusinessCountry(string country, string? expectedBusinessCountry)
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = "registered",
                OrganisationType = "DP",
                RegistrationYear = "2026",
                BusinessCountry = country
            };

            var result = WasteOrganisationsApiUpdateRequestMapper.Map(producer, _loggerMock.Object);

            result.BusinessCountry.Should().Be(expectedBusinessCountry);
        }

        [Theory]
        [InlineData("Registered", "DP", "REGISTERED", "LARGE_PRODUCER")]
        [InlineData("Deleted", "DP", "CANCELLED", "LARGE_PRODUCER")]
        [InlineData("Registered", "CS", "REGISTERED", "COMPLIANCE_SCHEME")]
        [InlineData("Deleted", "CS", "CANCELLED", "COMPLIANCE_SCHEME")]
        public void MapsRegistration(string status, string orgType, string expectedStatus, string expectedRegistrationType)
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = status,
                OrganisationType = orgType,
                RegistrationYear = "2026"
            };

            var result = WasteOrganisationsApiUpdateRequestMapper.Map(producer, _loggerMock.Object);

            result.Registration.Should().BeEquivalentTo(new Registration
            {
               Status = expectedStatus,
               Type = expectedRegistrationType,
               RegistrationYear = 2026,
            });
        }

        [Fact]
        public void ThrowsForUnrecognisedStatus()
        {
            Assert.Throws<ArgumentException>(() => WasteOrganisationsApiUpdateRequestMapper.Map(new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = "foobar",
                OrganisationType = "DP",
                RegistrationYear = "2026"
            }, _loggerMock.Object));
        }
        
        [Fact]
        public void ThrowsForUnrecognisedOrganisationType()
        {
            Assert.Throws<ArgumentException>(() => WasteOrganisationsApiUpdateRequestMapper.Map(new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = "registered",
                OrganisationType = "foobar",
                RegistrationYear = "2026"
            }, _loggerMock.Object));
        }

        [Fact]
        public void ThrowsForMissingId()
        {
            var producer = new UpdatedProducersResponseV2
            {
                OrganisationName = "some name",
                RegistrationYear = "2026"
            };

            Assert.Throws<ArgumentException>(() => WasteOrganisationsApiUpdateRequestMapper.Map(producer, _loggerMock.Object));
        }
        
        [Fact]
        public void ThrowsForMissingOrganisationName()
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                RegistrationYear = "2026"
            };

            Assert.Throws<ArgumentException>(() => WasteOrganisationsApiUpdateRequestMapper.Map(producer, _loggerMock.Object));
        }

        [Fact]
        public void ThrowsForInvalidRegistrationYear()
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = "registered",
                OrganisationType = "DP",
                RegistrationYear = "not-a-number"
            };

            Assert.Throws<ArgumentException>(() => WasteOrganisationsApiUpdateRequestMapper.Map(producer, _loggerMock.Object));
        }
    }
}