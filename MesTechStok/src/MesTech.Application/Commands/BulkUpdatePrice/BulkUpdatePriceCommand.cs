using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Commands.BulkUpdatePrice;

public record BulkUpdatePriceItem(string Sku, decimal NewPrice);

public record BulkUpdatePriceCommand(
    IReadOnlyList<BulkUpdatePriceItem> Items,
    Guid? TenantId = null
) : IRequest<BulkUpdateResult>;
