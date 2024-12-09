using EprPrnIntegration.Common.Models;
using FluentValidation;

namespace EprPrnIntegration.Common.Validators
{
    public class NpwdPrnValidator : AbstractValidator<NpwdPrn>
    {
        public NpwdPrnValidator()
        {
            RuleFor(prn => prn.EvidenceNo).NotNull().NotEmpty();
            RuleFor(prn => prn.IssuedToEPRId).NotNull().NotEmpty();
        }
    }
}
