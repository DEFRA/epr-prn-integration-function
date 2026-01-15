namespace EprPrnIntegration.Common.Configuration;

public interface ICognitoConfiguration
{
    string AccessTokenUrl { get; }
    string ClientId { get; }
    string ClientSecret { get; }
}
