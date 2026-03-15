using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.SearchProductsForImageMatch;

/// <summary>
/// Returns all active products as lightweight DTOs for image-to-product matching.
/// Used by ImageMapWizard to match filenames against Barcode/SKU fields.
/// </summary>
public record SearchProductsForImageMatchQuery : IRequest<IReadOnlyList<ProductDto>>;
