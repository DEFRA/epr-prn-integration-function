using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("EprPrnIntegration.Api.UnitTests")]

namespace EprPrnIntegration.Api
{
    [ExcludeFromCodeCoverage(Justification = "Required to expose internal methods to unit tests")]
    internal class AssemblyInfo
    {
    }
}