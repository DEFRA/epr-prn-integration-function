namespace EprPrnIntegration.Common.Helpers;

public static class StreamWriterExtensions
{
    public static async Task WriteCsvCellAsync(this StreamWriter writer, string value, bool isLastCell = false)
    {
        if (value == null)
        {
            await writer.WriteAsync(isLastCell ? "" : ",");
        }
        else
        {
             await writer.WriteAsync(value.CleanCsvString());

            // Only append a comma if this is not the last cell in the row
            if (!isLastCell)
            {
                await writer.WriteAsync(",");
            }
        }
    }
}