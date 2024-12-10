using AutoFixture;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Validators;
using FluentValidation.TestHelper;

namespace EprPrnIntegration.Common.UnitTests.Validators
{
    public class NpwdPrnValidatorTests
    {
        private NpwdPrnValidator _sut = null!;
        private Fixture _fixture = new Fixture();
        public NpwdPrnValidatorTests()
        {
            _sut = new NpwdPrnValidator();
        }

        [Fact]
        public void Should_Not_Have_Error_When_EvidenceNo_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.EvidenceNo);
        }
        [Fact]
        public void Should_Have_Error_When_EvidenceNo_Is_Not_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceNo = string.Empty;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceNo);
        }

        [Fact]
        public void Should_Not_Have_Error_When_IssuedEPRId_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.IssuedToEPRId);
        }
        [Fact]
        public void Should_Have_Error_When_IssuedEPRId_Is_Not_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.IssuedToEPRId = string.Empty;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.IssuedToEPRId);
        }

    }
}
