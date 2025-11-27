using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationApi;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Mappers
{
    public class OrganisationUpdateRequestMapperTests
    {
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
                
                SubmissionYear = 2025,
                Status = "registered",
                OrganisationType = "DP"
            };
            
            var result = OrganisationUpdateRequestMapper.Map(input);
            
            result.Id.Should().Be(input.PEPRID);
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
                SubmissionYear = 2025,
                
                CompaniesHouseNumber = "some-company-number",
                OrganisationName = "Organisation's Name",
                TradingName = "Organisation's TradingName"
            };
            
            var result = OrganisationUpdateRequestMapper.Map(input);

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
        [InlineData("England", BusinessCountry.England)]
        [InlineData("Northern Ireland", BusinessCountry.NorthernIreland)]
        [InlineData("Wales", BusinessCountry.Wales)]
        [InlineData("Scotland", BusinessCountry.Scotland)]
        [InlineData("", null)]
        public void MapsBusinessCountry(string country, BusinessCountry? expectedBusinessCountry)
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = "registered",
                OrganisationType = "DP",
                SubmissionYear = 2026,
                BusinessCountry = country
            };
        
            var result = OrganisationUpdateRequestMapper.Map(producer);
            
            result.BusinessCountry.Should().Be(expectedBusinessCountry);
        }

        [Theory]
        [InlineData("registered", "DP", RegistrationStatus.Registered, RegistrationType.LargeProducer)]
        [InlineData("deleted", "DP", RegistrationStatus.Cancelled, RegistrationType.LargeProducer)]
        [InlineData("registered", "S", RegistrationStatus.Registered, RegistrationType.ComplianceScheme)]
        [InlineData("deleted", "S", RegistrationStatus.Cancelled, RegistrationType.ComplianceScheme)]
        public void MapsRegistration(string status, string orgType, RegistrationStatus expectedStatus, RegistrationType expectedRegistrationType)
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = status,
                OrganisationType = orgType,
                SubmissionYear = 2026
            };
        
            var result = OrganisationUpdateRequestMapper.Map(producer);
            
            result.Registration.Should().BeEquivalentTo(new Registration
            {
               Status = expectedStatus,
               Type = expectedRegistrationType,
               SubmissionYear = 2026,
            });
        }

        [Fact]
        public void ThrowsForUnrecognisedStatus()
        {
            Assert.Throws<ArgumentException>(() => OrganisationUpdateRequestMapper.Map(new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = "foobar",
                OrganisationType = "DP",
                SubmissionYear = 2026
            }));
        }
        
        [Fact]
        public void ThrowsForUnrecognisedOrganisationType()
        {
            Assert.Throws<ArgumentException>(() => OrganisationUpdateRequestMapper.Map(new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = "registered",
                OrganisationType = "foobar",
                SubmissionYear = 2026
            }));
        }
        
        [Fact]
        public void ThrowsForMissingOrganisationYear()
        {
            Assert.Throws<ArgumentException>(() => OrganisationUpdateRequestMapper.Map(new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = "CS Added",
                OrganisationType = "S",
                SubmissionYear = null
            }));
        }

        [Fact]
        public void ThrowsForMissingId()
        {
            var producer = new UpdatedProducersResponseV2
            {
                OrganisationName = "some name"
            };
            
            Assert.Throws<ArgumentException>(() => OrganisationUpdateRequestMapper.Map(producer));
        }
        
        [Fact]
        public void ThrowsForMissingOrganisationName()
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
            };
            
            Assert.Throws<ArgumentException>(() => OrganisationUpdateRequestMapper.Map(producer));
        }
    }
}