namespace MesTech.Application.Features.Reporting.Commands.ExportReport;

/// <summary>
/// Genel rapor disa aktarma sonucu.
/// </summary>
public sealed class ExportReportResult
{
    public byte[] FileData { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public int ExportedCount { get; set; }
}
