using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Commands.BulkUpdateStock;

public record BulkUpdateStockItem(string Sku, int NewStock);

public record BulkUpdateStockCommand(
    IReadOnlyList<BulkUpdateStockItem> Items,
    Guid? TenantId = null
) : IRequest<BulkUpdateResult>;
