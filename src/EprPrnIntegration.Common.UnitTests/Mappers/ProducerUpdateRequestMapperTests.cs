using AutoFixture;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Models.Rrepw;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Mappers
{
    public class ProducerUpdateRequestMapperTests
    {
        [Fact]
        public void MapsCorrectFields()
        {
            var input = new UpdatedProducersResponse()
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
            
            var result = ProducerUpdateRequestMapper.Map(input);

            var expected = new ProducerUpdateRequest()
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
            var producer = new UpdatedProducersResponse
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = status,
                OrganisationType = orgType,
            };
        
            var result = ProducerUpdateRequestMapper.Map(producer);
        
            Assert.Equal(result.Status, expectedStatusCode);
            Assert.Equal(result.Type, expectedProducerType);
        }

        [Fact]
        public void ThrowsForMissingId()
        {
            var producer = new UpdatedProducersResponse
            {
                OrganisationName = "some name"
            };
            
            Assert.Throws<ArgumentException>(() => ProducerUpdateRequestMapper.Map(producer));
        }
        
        [Fact]
        public void ThrowsForMissingOrganisationName()
        {
            var producer = new UpdatedProducersResponse
            {
                PEPRID = Guid.NewGuid().ToString(),
            };
            
            Assert.Throws<ArgumentException>(() => ProducerUpdateRequestMapper.Map(producer));
        }
    }
}