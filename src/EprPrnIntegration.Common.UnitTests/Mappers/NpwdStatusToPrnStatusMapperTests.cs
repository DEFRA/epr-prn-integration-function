using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Mappers;

namespace EprPrnIntegration.Tests.Mappers
{
    public class NpwdStatusToPrnStatusMapperTests
    {
        [Theory]
        [InlineData("EV-ACCEP", EprnStatus.ACCEPTED)]
        [InlineData("EV-ACANCEL", EprnStatus.REJECTED)]
        [InlineData("EV-CANCEL", EprnStatus.CANCELLED)]
        [InlineData("EV-AWACCEP", EprnStatus.AWAITINGACCEPTANCE)]
        [InlineData("EV-AWACCEP-EPR", EprnStatus.AWAITINGACCEPTANCE)]
        [InlineData("ev-accep", EprnStatus.ACCEPTED)] // Testing lower case
        public void Map_ValidStatuses_ReturnsExpectedEnum(
            string npwdStatus,
            EprnStatus expectedStatus
        )
        {
            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Equal(expectedStatus, result);
        }

        [Theory]
        [InlineData("EV-INVALID")]
        [InlineData(null)]
        [InlineData("")]
        public void Map_InvalidOrNullStatuses_ReturnsNull(string npwdStatus)
        {
            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Null(result);
        }
    }
}
