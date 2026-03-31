namespace MesTech.Application.Features.Crm.Commands.ExportCustomers;

/// <summary>
/// Musteri disa aktarma sonucu.
/// </summary>
public sealed class ExportCustomersResult
{
    public byte[] FileData { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public int ExportedCount { get; set; }
}
