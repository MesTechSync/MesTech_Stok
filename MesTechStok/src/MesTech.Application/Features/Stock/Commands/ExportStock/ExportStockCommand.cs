using MediatR;

namespace MesTech.Application.Features.Stock.Commands.ExportStock;

/// <summary>
/// Stok verilerini disa aktarma komutu — ExportAvaloniaViewModel.ExportAsync().
/// </summary>
public sealed record ExportStockCommand(
    Guid TenantId,
    string Format = "xlsx",
    string? Filter = null
) : IRequest<ExportStockResult>;
