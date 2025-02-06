namespace EprPrnIntegration.Common.Helpers;

public static class StringExtensions
{
    public static string CleanCsvString(this string value)
    {
        string escapedValue;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            escapedValue = $"\"{value.Replace("\"", "\"\"")}\"";
        }
        else
        {
            escapedValue = value;
        }
        return escapedValue;

    }
}
