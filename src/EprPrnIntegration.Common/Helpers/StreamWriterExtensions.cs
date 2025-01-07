namespace EprPrnIntegration.Common.Helpers;

public static class StreamWriterExtensions
{
    public static async Task WriteCsvCellAsync(this StreamWriter writer, string value)
    {
        if (value == null)
        {
            await writer.WriteAsync(",");
        }
        else
        {
            var escapedValue = value.Contains(",") || value.Contains("\"") || value.Contains("\n")
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;

            await writer.WriteAsync(escapedValue + ",");
        }
    }

    public static async Task WriteLineAsync(this StreamWriter writer)
    {
        await writer.WriteAsync("\r\n");
    }
}