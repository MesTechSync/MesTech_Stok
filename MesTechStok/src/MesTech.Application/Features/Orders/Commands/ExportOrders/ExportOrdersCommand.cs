using MediatR;

namespace MesTech.Application.Features.Orders.Commands.ExportOrders;

/// <summary>
/// Siparişleri Excel'e dışa aktar — Ekran 12 Dışa Aktar.
/// </summary>
public record ExportOrdersCommand(
    Guid TenantId,
    DateTime From,
    DateTime To,
    string? PlatformFilter = null
) : IRequest<ExportOrdersResult>;

public sealed class ExportOrdersResult
{
    public bool IsSuccess { get; set; }
    public ReadOnlyMemory<byte>? FileContent { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ExportedCount { get; set; }
    public string? ErrorMessage { get; set; }
}
