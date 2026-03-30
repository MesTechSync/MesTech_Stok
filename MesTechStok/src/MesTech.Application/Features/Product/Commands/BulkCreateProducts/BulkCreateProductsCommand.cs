using MediatR;

namespace MesTech.Application.Features.Product.Commands.BulkCreateProducts;

/// <summary>
/// CSV/Excel dosyasindan toplu urun olusturma komutu.
/// </summary>
public record BulkCreateProductsCommand(
    Guid TenantId,
    IReadOnlyList<BulkProductInput> Products
) : IRequest<BulkCreateProductsResult>;
