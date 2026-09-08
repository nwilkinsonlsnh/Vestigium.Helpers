namespace Vestigium.Helpers.Csv;

/// <summary>RFC 4180 parse failure. <see cref="LineNumber"/> is 1-based.</summary>
public sealed class CsvFormatException : FormatException
{
    public int LineNumber { get; }

    public CsvFormatException(int lineNumber, string message)
        : base($"Line {lineNumber}: {message}")
    {
        LineNumber = lineNumber;
    }

    public CsvFormatException(int lineNumber, string message, Exception inner)
        : base($"Line {lineNumber}: {message}", inner)
    {
        LineNumber = lineNumber;
    }
}
