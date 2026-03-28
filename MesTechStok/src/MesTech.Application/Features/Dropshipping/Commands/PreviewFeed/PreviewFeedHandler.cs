using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;

public sealed class PreviewFeedHandler : IRequestHandler<PreviewFeedCommand, FeedPreviewDto>
{
    private readonly ISupplierFeedRepository _feedRepository;
    private readonly IDropshipProductRepository _productRepository;
    private readonly IEnumerable<IFeedParserService> _feedParsers;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PreviewFeedHandler> _logger;

    public PreviewFeedHandler(
        ISupplierFeedRepository feedRepository,
        IDropshipProductRepository productRepository,
        IEnumerable<IFeedParserService> feedParsers,
        IHttpClientFactory httpClientFactory,
        ILogger<PreviewFeedHandler> logger)
    {
        _feedRepository = feedRepository;
        _productRepository = productRepository;
        _feedParsers = feedParsers;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FeedPreviewDto> Handle(
        PreviewFeedCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var feed = await _feedRepository.GetByIdAsync(request.FeedSourceId, cancellationToken);
        if (feed is null)
            throw new InvalidOperationException($"SupplierFeed {request.FeedSourceId} not found.");

        var parser = _feedParsers.FirstOrDefault(p => p.SupportedFormat == feed.Format);
        if (parser is null)
            return new FeedPreviewDto { Warnings = { $"No parser available for feed format: {feed.Format}" } };

        var (parseResult, downloadError) = await DownloadAndParseAsync(feed.FeedUrl, parser, cancellationToken);
        if (downloadError is not null)
        {
            _logger.LogError("Failed to download/parse feed {FeedId}: {Error}", request.FeedSourceId, downloadError);
            return new FeedPreviewDto { Warnings = { $"Failed to download or parse feed: {downloadError}" } };
        }

        if (parseResult is null)
            return new FeedPreviewDto { Warnings = { "Parse result was empty." } };

        return await BuildPreviewAsync(feed, parseResult, cancellationToken);
    }

    private async Task<(FeedParseResult? Result, string? Error)> DownloadAndParseAsync(
        string feedUrl, IFeedParserService parser, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("FeedDownload");
            using var stream = await httpClient.GetStreamAsync(new Uri(feedUrl), cancellationToken).ConfigureAwait(false);
            var mapping = new FeedFieldMapping(null, null, null, null, null, null, null, null);
            var result = await parser.ParseAsync(stream, mapping, cancellationToken);
            return (result, null);
        }
        catch (HttpRequestException ex)
        {
            return (null, ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            return (null, ex.Message);
        }
    }

    private async Task<FeedPreviewDto> BuildPreviewAsync(
        SupplierFeed feed,
        FeedParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var existingProducts = await _productRepository
            .GetBySupplierAsync(feed.SupplierId, cancellationToken);
        var existingSkus = existingProducts
            .Select(p => p.ExternalProductId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var previewProducts = parseResult.Products
            .Take(50)
            .Select(p => new FeedProductPreviewDto
            {
                Name = p.Name ?? string.Empty,
                SKU = p.SKU,
                SupplierPrice = p.Price ?? 0m,
                SuggestedPrice = feed.ApplyMarkup(p.Price ?? 0m),
                Stock = p.Quantity ?? 0,
                AlreadyExists = p.SKU is not null && existingSkus.Contains(p.SKU)
            })
            .ToList();

        var warnings = new List<string>(parseResult.Errors);
        if (parseResult.SkippedCount > 0)
            warnings.Add($"{parseResult.SkippedCount} products skipped during parsing.");

        return new FeedPreviewDto
        {
            TotalProductCount = parseResult.TotalParsed,
            Products = previewProducts,
            Warnings = warnings
        };
    }
}
