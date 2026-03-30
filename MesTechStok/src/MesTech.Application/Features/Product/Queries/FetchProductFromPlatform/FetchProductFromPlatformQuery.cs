using MesTech.Application.DTOs.Invoice;
using MediatR;

namespace MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;

/// <summary>
/// Platform URL'sinden ürün bilgisi çeker.
/// IProductScraperService üzerinden adapter API'larını kullanır.
/// Avalonia ProductFetchAvaloniaViewModel bu query'yi kullanır.
/// </summary>
public record FetchProductFromPlatformQuery(
    string ProductUrl) : IRequest<ScrapedProductDto?>;
