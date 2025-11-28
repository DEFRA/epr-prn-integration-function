using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EprPrnIntegration.Api.IntegrationTests;

/// <summary>
/// Helper class to retrieve environment variables from running Docker containers.
/// </summary>
public static class DockerEnvironmentHelper
{
    /// <summary>
    /// Gets the AzureWebJobsStorage connection string from the prn-functions container.
    /// Throws an exception if the container is not running or the environment variable is not set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve the value from Docker</exception>
    public static string GetAzureWebJobsStorage()
    {
        var connectionString = GetEnvironmentVariableFromContainer("prn-functions", "AzureWebJobsStorage");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Failed to retrieve AzureWebJobsStorage from the prn-functions container. " +
                "Ensure the container is running with 'docker compose up -d prn-functions'");
        }

        // Replace container-internal azurite hostname with localhost for tests running outside Docker
        return connectionString
            .Replace("azurite:10000", "localhost:10000")
            .Replace("azurite:10001", "localhost:10001");
    }

    /// <summary>
    /// Retrieves an environment variable from a running Docker container.
    /// </summary>
    /// <param name="containerName">Name of the Docker container</param>
    /// <param name="environmentVariable">Name of the environment variable</param>
    /// <returns>The value of the environment variable, or null if not found</returns>
    private static string? GetEnvironmentVariableFromContainer(string containerName, string environmentVariable)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"compose exec {containerName} printenv {environmentVariable}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
        {
            return output.Trim();
        }

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Docker command failed: {error}");
        }

        return null;
    }
}
