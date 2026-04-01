namespace MesTech.Application.Features.Invoice.Commands.ExportInvoices;

/// <summary>
/// Fatura disa aktarma sonucu.
/// </summary>
public sealed class ExportInvoicesResult
{
    public ReadOnlyMemory<byte> FileData { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ExportedCount { get; set; }
}
