namespace MesTech.Application.Features.Stock.Commands.ExportStock;

/// <summary>
/// Stok disa aktarma sonucu.
/// </summary>
public sealed class ExportStockResult
{
    public byte[] FileData { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public int ExportedCount { get; set; }
}
