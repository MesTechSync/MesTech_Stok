using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Product.Commands.ExportProducts;

/// <summary>
/// Ürünleri Excel formatında dışa aktarma komutu.
/// </summary>
public record ExportProductsCommand(
    PlatformType? Platform = null,
    Guid? CategoryId = null,
    bool? InStock = null,
    string Format = "xlsx"
) : IRequest<byte[]>;
