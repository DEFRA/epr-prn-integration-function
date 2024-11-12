namespace EprPrnIntegration.Common.Service
{
    public interface IConfigurationService
    {
        string? NpwdAccessTokenUrl { get; }
        string? NpwdAuthority { get; }
        string? NpwdClientId { get; }
        string? NpwdClientSecret { get; }
        string? NpwdScope { get; }
        string? NpwdAccessTokenName { get; }

        string? GetNpwdApiBaseUrl();
    }
}