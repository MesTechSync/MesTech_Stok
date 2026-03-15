using MediatR;

namespace MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;

/// <summary>
/// Dropship ürününü MesTech ürünüyle eşleştirir.
/// </summary>
public record LinkDropshipProductCommand(
    Guid TenantId,
    Guid DropshipProductId,
    Guid MesTechProductId
) : IRequest<Unit>;
