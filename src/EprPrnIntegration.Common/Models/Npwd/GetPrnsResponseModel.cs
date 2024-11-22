using System.Diagnostics.CodeAnalysis;

namespace EprPrnIntegration.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class GetPrnsResponseModel
    {
        public List<NpwdPrn> Value { get; set; } = [];
    }
}
