using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;

public class FetchProductFromUrlHandler
    : IRequestHandler<FetchProductFromUrlCommand, FetchedProductDto?>
{
    private readonly ILogger<FetchProductFromUrlHandler> _logger;

    /// <summary>
    /// Domain-to-PlatformType mapping for URL parsing.
    /// </summary>
    private static readonly Dictionary<string, PlatformType> DomainMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trendyol.com"] = PlatformType.Trendyol,
        ["hepsiburada.com"] = PlatformType.Hepsiburada,
        ["n11.com"] = PlatformType.N11,
        ["ciceksepeti.com"] = PlatformType.Ciceksepeti,
        ["pazarama.com"] = PlatformType.Pazarama,
        ["amazon.com.tr"] = PlatformType.Amazon,
        ["amazon.de"] = PlatformType.AmazonEu,
        ["amazon.co.uk"] = PlatformType.AmazonEu,
        ["ebay.com"] = PlatformType.eBay,
        ["ozon.ru"] = PlatformType.Ozon,
        ["pttavm.com"] = PlatformType.PttAVM,
        ["etsy.com"] = PlatformType.Etsy,
    };

    public FetchProductFromUrlHandler(ILogger<FetchProductFromUrlHandler> logger)
    {
        _logger = logger;
    }

    public Task<FetchedProductDto?> Handle(
        FetchProductFromUrlCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProductUrl))
        {
            _logger.LogWarning("FetchProductFromUrl: Empty URL provided.");
            return Task.FromResult<FetchedProductDto?>(null);
        }

        if (!Uri.TryCreate(request.ProductUrl, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("FetchProductFromUrl: Invalid URL: {Url}", request.ProductUrl);
            return Task.FromResult<FetchedProductDto?>(null);
        }

        // Match domain to platform
        PlatformType? matchedPlatform = null;
        foreach (var kvp in DomainMap)
        {
            if (uri.Host.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                matchedPlatform = kvp.Value;
                break;
            }
        }

        if (matchedPlatform is null)
        {
            _logger.LogWarning(
                "FetchProductFromUrl: No platform matched for host {Host}",
                uri.Host);
            return Task.FromResult<FetchedProductDto?>(null);
        }

        // Placeholder: platform identified, actual scraping will be implemented per platform adapter.
        var dto = new FetchedProductDto
        {
            Name = $"[{matchedPlatform}] Product from {uri.Host}",
            Price = 0m,
            Description = $"Placeholder — actual scraping for {matchedPlatform} not yet implemented.",
            Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Platform"] = matchedPlatform.ToString() ?? string.Empty,
                ["SourceUrl"] = request.ProductUrl,
            }
        };

        _logger.LogInformation(
            "FetchProductFromUrl: Identified platform {Platform} for URL {Url}",
            matchedPlatform, request.ProductUrl);

        return Task.FromResult<FetchedProductDto?>(dto);
    }
}
