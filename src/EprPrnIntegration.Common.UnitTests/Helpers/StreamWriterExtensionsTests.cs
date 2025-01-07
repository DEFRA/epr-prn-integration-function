using EprPrnIntegration.Common.Helpers;
using System.Text;

namespace EprPrnIntegration.Common.UnitTests.Helpers;

public class StreamWriterExtensionsTests
{
    [Fact]
    public async Task WriteCsvCellAsync_ShouldWriteSimpleValueCorrectly()
    {
        // Arrange
        var stringBuilder = new StringBuilder();
        using var writer = new StreamWriter(new MemoryStream());

        // Act
        await writer.WriteCsvCellAsync("SimpleValue");
        await writer.FlushAsync();

        // Assert
        writer.BaseStream.Position = 0;
        var result = await new StreamReader(writer.BaseStream).ReadToEndAsync();
        Assert.Equal("SimpleValue,", result);
    }

    [Fact]
    public async Task WriteCsvCellAsync_ShouldEscapeCommas()
    {
        // Arrange
        var stringBuilder = new StringBuilder();
        using var writer = new StreamWriter(new MemoryStream());

        // Act
        await writer.WriteCsvCellAsync("Value,With,Comma");
        await writer.FlushAsync();

        // Assert
        writer.BaseStream.Position = 0;
        var result = await new StreamReader(writer.BaseStream).ReadToEndAsync();
        Assert.Equal("\"Value,With,Comma\",", result);
    }

    [Fact]
    public async Task WriteCsvCellAsync_ShouldEscapeQuotes()
    {
        // Arrange
        var stringBuilder = new StringBuilder();
        using var writer = new StreamWriter(new MemoryStream());

        // Act
        await writer.WriteCsvCellAsync("Value\"With\"Quotes");
        await writer.FlushAsync();

        // Assert
        writer.BaseStream.Position = 0;
        var result = await new StreamReader(writer.BaseStream).ReadToEndAsync();
        Assert.Equal("\"Value\"\"With\"\"Quotes\",", result);
    }

    [Fact]
    public async Task WriteCsvCellAsync_ShouldHandleNullValue()
    {
        // Arrange
        using var writer = new StreamWriter(new MemoryStream());

        // Act
        await writer.WriteCsvCellAsync(null);
        await writer.FlushAsync();

        // Assert
        writer.BaseStream.Position = 0;
        var result = await new StreamReader(writer.BaseStream).ReadToEndAsync();
        Assert.Equal(",", result);
    }

    [Fact]
    public async Task WriteLineAsync_ShouldWriteNewLine()
    {
        // Arrange
        using var writer = new StreamWriter(new MemoryStream());

        // Act
        await writer.WriteLineAsync();
        await writer.FlushAsync();

        // Assert
        writer.BaseStream.Position = 0;
        var result = await new StreamReader(writer.BaseStream).ReadToEndAsync();
        Assert.Equal("\r\n", result);
    }
}
