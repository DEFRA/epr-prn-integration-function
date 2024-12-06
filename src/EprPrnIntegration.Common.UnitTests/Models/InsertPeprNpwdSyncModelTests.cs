using AutoFixture;
using EprPrnIntegration.Common.Models;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Models
{
    public class InsertPeprNpwdSyncModelTests
    {
        private readonly Fixture _fixture = new();
        
        [Theory]
        [InlineData("EV-ACCEP","ACCEPTED")]
        [InlineData("EV-ACANCEL","REJECTED")]
        public void Correctly_Map_UpdatePrnsResponseModel(string npwdStatus, string peprStatus)
        {
            var updatePrn = _fixture.Create<UpdatedPrnsResponseModel>();
            updatePrn.EvidenceStatusCode = npwdStatus;

            var insertSync = (InsertPeprNpwdSyncModel)updatePrn;

            insertSync.EvidenceStatus.Should().Be(peprStatus);
            insertSync.EvidenceNo.Should().Be(updatePrn.EvidenceNo);
        }

        [Fact]
        public void Throws_InvalidDataException_IfUnknownStatus()
        {
            var updatePrn = _fixture.Create<UpdatedPrnsResponseModel>();

            FluentActions.Invoking(() => (InsertPeprNpwdSyncModel)updatePrn).Should().Throw<InvalidDataException>();
        }
    }
}
