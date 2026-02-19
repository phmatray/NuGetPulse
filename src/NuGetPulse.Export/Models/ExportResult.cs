namespace NuGetPulse.Export.Models;

/// <summary>The result of an export operation.</summary>
public sealed class ExportResult
{
    public ExportFormat Format { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty;
    public byte[] BinaryData { get; init; } = [];
    public long DataSize => BinaryData.Length;
}

public enum ExportFormat
{
    Csv,
    Json
}
