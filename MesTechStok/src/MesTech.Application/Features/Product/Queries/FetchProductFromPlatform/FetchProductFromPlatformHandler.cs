using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;

public sealed class FetchProductFromPlatformHandler
    : IRequestHandler<FetchProductFromPlatformQuery, ScrapedProductDto?>
{
    private readonly IProductScraperService _scraperService;
    private readonly ILogger<FetchProductFromPlatformHandler> _logger;

    public FetchProductFromPlatformHandler(
        IProductScraperService scraperService,
        ILogger<FetchProductFromPlatformHandler> logger)
    {
        _scraperService = scraperService;
        _logger = logger;
    }

    public async Task<ScrapedProductDto?> Handle(
        FetchProductFromPlatformQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "FetchProductFromPlatform: URL={Url}", request.ProductUrl);

        var result = await _scraperService
            .ScrapeFromUrlAsync(request.ProductUrl, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            _logger.LogWarning(
                "FetchProductFromPlatform: product not found for URL={Url}", request.ProductUrl);
        }
        else
        {
            _logger.LogInformation(
                "FetchProductFromPlatform: found {Title} ({Platform}) — {Price:C}",
                result.Title, result.Platform, result.Price);
        }

        return result;
    }
}
