using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using FluentAssertions;

namespace EprPrnIntegration.Common.UnitTests.Mappers
{
    public class WasteOrganisationsApiUpdateRequestMapperTests
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

                RegistrationYear = "2025",
                Status = OrganisationStatus.Registered,
                OrganisationType = OrganisationType.LargeProducer_DP,
            };

            var result = WasteOrganisationsApiUpdateRequestMapper.Map(input);

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

                Status = OrganisationStatus.Registered,
                OrganisationType = OrganisationType.LargeProducer_DP,
                RegistrationYear = "2025",

                CompaniesHouseNumber = "some-company-number",
                OrganisationName = "Organisation's Name",
                TradingName = "Organisation's TradingName",
            };

            var result = WasteOrganisationsApiUpdateRequestMapper.Map(input);

            result
                .Address.Should()
                .BeEquivalentTo(
                    new Common.Models.WasteOrganisationsApi.Address
                    {
                        AddressLine1 = "address1",
                        AddressLine2 = "address2",
                        Town = "town",
                        County = "county",
                        Postcode = "postcode",
                        Country = "UK",
                    }
                );
        }

        [Theory]
        [InlineData(BusinessCountry.England, WoApiBusinessCountry.England)]
        [InlineData(BusinessCountry.NorthernIreland, WoApiBusinessCountry.NorthernIreland)]
        [InlineData(BusinessCountry.Wales, WoApiBusinessCountry.Wales)]
        [InlineData(BusinessCountry.Scotland, WoApiBusinessCountry.Scotland)]
        [InlineData("", null)]
        public void MapsBusinessCountry(string country, string? expectedBusinessCountry)
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = OrganisationStatus.Registered,
                OrganisationType = OrganisationType.LargeProducer_DP,
                RegistrationYear = "2026",
                BusinessCountry = country,
            };

            var result = WasteOrganisationsApiUpdateRequestMapper.Map(producer);

            result.BusinessCountry.Should().Be(expectedBusinessCountry);
        }

        [Theory]
        [InlineData(
            OrganisationStatus.Registered,
            OrganisationType.LargeProducer_DP,
            WoApiOrganisationStatus.Registered,
            WoApiOrganisationType.LargeProducer
        )]
        [InlineData(
            OrganisationStatus.Deleted,
            OrganisationType.LargeProducer_DP,
            WoApiOrganisationStatus.Cancelled,
            WoApiOrganisationType.LargeProducer
        )]
        [InlineData(
            OrganisationStatus.Registered,
            OrganisationType.ComplianceScheme_CS,
            WoApiOrganisationStatus.Registered,
            WoApiOrganisationType.ComplianceScheme
        )]
        [InlineData(
            OrganisationStatus.Deleted,
            OrganisationType.ComplianceScheme_CS,
            WoApiOrganisationStatus.Cancelled,
            WoApiOrganisationType.ComplianceScheme
        )]
        public void MapsRegistration(
            string status,
            string orgType,
            string expectedStatus,
            string expectedRegistrationType
        )
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = status,
                OrganisationType = orgType,
                RegistrationYear = "2026",
            };

            var result = WasteOrganisationsApiUpdateRequestMapper.Map(producer);

            result
                .Registration.Should()
                .BeEquivalentTo(
                    new Registration
                    {
                        Status = expectedStatus,
                        Type = expectedRegistrationType,
                        RegistrationYear = 2026,
                    }
                );
        }

        [Fact]
        public void ThrowsForUnrecognisedStatus()
        {
            Assert.Throws<ArgumentException>(() =>
                WasteOrganisationsApiUpdateRequestMapper.Map(
                    new UpdatedProducersResponseV2
                    {
                        PEPRID = Guid.NewGuid().ToString(),
                        OrganisationName = Guid.NewGuid().ToString(),
                        Status = "foobar",
                        OrganisationType = OrganisationType.LargeProducer_DP,
                        RegistrationYear = "2026",
                    }
                )
            );
        }

        [Fact]
        public void ThrowsForUnrecognisedOrganisationType()
        {
            Assert.Throws<ArgumentException>(() =>
                WasteOrganisationsApiUpdateRequestMapper.Map(
                    new UpdatedProducersResponseV2
                    {
                        PEPRID = Guid.NewGuid().ToString(),
                        OrganisationName = Guid.NewGuid().ToString(),
                        Status = OrganisationStatus.Registered,
                        OrganisationType = "foobar",
                        RegistrationYear = "2026",
                    }
                )
            );
        }

        [Fact]
        public void ThrowsForMissingId()
        {
            var producer = new UpdatedProducersResponseV2
            {
                OrganisationName = "some name",
                RegistrationYear = "2026",
            };

            Assert.Throws<ArgumentException>(() =>
                WasteOrganisationsApiUpdateRequestMapper.Map(producer)
            );
        }

        [Fact]
        public void ThrowsForMissingOrganisationName()
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                RegistrationYear = "2026",
            };

            Assert.Throws<ArgumentException>(() =>
                WasteOrganisationsApiUpdateRequestMapper.Map(producer)
            );
        }

        [Fact]
        public void ThrowsForInvalidRegistrationYear()
        {
            var producer = new UpdatedProducersResponseV2
            {
                PEPRID = Guid.NewGuid().ToString(),
                OrganisationName = Guid.NewGuid().ToString(),
                Status = OrganisationStatus.Registered,
                OrganisationType = OrganisationType.LargeProducer_DP,
                RegistrationYear = "not-a-number",
            };

            Assert.Throws<ArgumentException>(() =>
                WasteOrganisationsApiUpdateRequestMapper.Map(producer)
            );
        }
    }
}
