using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.WasteOrganisationApi;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Mappers
{
    public class OrganisationUpdateRequestMapperTests
    {
        [Fact]
        public void MapsCorrectFields()
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
                
                OrganisationName = "Organisation's Name",
                TradingName = "Compliance Scheme's name",
            };
            
            var result = OrganisationUpdateRequestMapper.Map(input);

            var expected = new OrganisationUpdateRequest
            {
                Id = input.PEPRID,
                AddressLine1 = "address1",
                AddressLine2 = "address2",
                Town = "town",
                County = "county",
                Postcode = "postcode",
                Country = "UK",
                
                Name = "Organisation's Name",
                TradingName = "Compliance Scheme's name",
            };
            
            result.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData("DR Registered", "DR", ProducerStatus.PrRegistered, ProducerType.DR)]
        [InlineData("DR Deleted", "DR", ProducerStatus.PrCancelled, ProducerType.DR)]
        [InlineData("CS Added", "S", ProducerStatus.CsrRegistered, ProducerType.CS)]
        [InlineData("CS Deleted", "S", ProducerStatus.CsrCancelled, ProducerType.CS)]
        [InlineData("Some Unmatched Status", "Some Organisation Type", null, null)]
        public void MapsCorrectStatusCodes(string status, string orgType, ProducerStatus? expectedStatusCode, ProducerType? expectedProducerType)
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = status,
                OrganisationType = orgType,
            };
        
            var result = OrganisationUpdateRequestMapper.Map(producer);
        
            Assert.Equal(result.Status, expectedStatusCode);
            Assert.Equal(result.Type, expectedProducerType);
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