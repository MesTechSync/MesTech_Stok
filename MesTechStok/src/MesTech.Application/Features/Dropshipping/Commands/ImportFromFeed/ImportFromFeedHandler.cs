using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;

public sealed class ImportFromFeedHandler : IRequestHandler<ImportFromFeedCommand, ImportResultDto>
{
    private readonly ISupplierFeedRepository _feedRepository;
    private readonly IDropshipProductRepository _productRepository;
    private readonly IUnitOfWork _uow;
    private readonly IEnumerable<IFeedParserService> _feedParsers;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImportFromFeedHandler> _logger;

    public ImportFromFeedHandler(
        ISupplierFeedRepository feedRepository,
        IDropshipProductRepository productRepository,
        IUnitOfWork uow,
        IEnumerable<IFeedParserService> feedParsers,
        IHttpClientFactory httpClientFactory,
        ILogger<ImportFromFeedHandler> logger)
    {
        _feedRepository = feedRepository;
        _productRepository = productRepository;
        _uow = uow;
        _feedParsers = feedParsers;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ImportResultDto> Handle(
        ImportFromFeedCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = new ImportResultDto();

        var feed = await _feedRepository.GetByIdAsync(request.FeedSourceId, cancellationToken);
        if (feed is null)
            throw new InvalidOperationException($"SupplierFeed {request.FeedSourceId} not found.");

        var parser = _feedParsers.FirstOrDefault(p => p.SupportedFormat == feed.Format);
        if (parser is null)
        {
            result.Errors.Add($"No parser available for feed format: {feed.Format}");
            return result;
        }

        var (parseResult, downloadError) = await DownloadAndParseAsync(feed.FeedUrl, parser, cancellationToken);
        if (downloadError is not null)
        {
            _logger.LogError("Failed to download/parse feed {FeedId}: {Error}", request.FeedSourceId, downloadError);
            result.Errors.Add($"Failed to download or parse feed: {downloadError}");
            return result;
        }

        if (parseResult is null)
        {
            result.Errors.Add("Parse result was empty.");
            return result;
        }

        await ImportProductsAsync(feed, parseResult, request, result, cancellationToken);

        _logger.LogInformation(
            "ImportFromFeed {FeedId}: Imported {Count}/{Total}, Skipped {Skipped}",
            request.FeedSourceId, result.ImportedCount, result.TotalProcessed, result.SkippedCount);

        return result;
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

    private async Task ImportProductsAsync(
        SupplierFeed feed,
        FeedParseResult parseResult,
        ImportFromFeedCommand request,
        ImportResultDto result,
        CancellationToken cancellationToken)
    {
        var selectedSet = request.SelectedSkus.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filtered = parseResult.Products
            .Where(p => p.SKU is not null && selectedSet.Contains(p.SKU))
            .ToList();

        var newProducts = new List<DropshipProduct>();
        foreach (var parsed in filtered)
        {
            result.TotalProcessed++;

            if (string.IsNullOrWhiteSpace(parsed.SKU) || string.IsNullOrWhiteSpace(parsed.Name))
            {
                result.SkippedCount++;
                continue;
            }

            TryCreateProduct(feed, parsed, request.PriceMultiplier, result, newProducts);
        }

        if (newProducts.Count > 0)
        {
            await _productRepository.AddRangeAsync(newProducts, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }

    private static void TryCreateProduct(
        SupplierFeed feed,
        ParsedProduct parsed,
        decimal priceMultiplier,
        ImportResultDto result,
        List<DropshipProduct> newProducts)
    {
        try
        {
            var sku = parsed.SKU ?? string.Empty;
            var name = parsed.Name ?? string.Empty;
            var product = DropshipProduct.Create(
                feed.TenantId,
                feed.SupplierId,
                sku,
                name,
                parsed.Price ?? 0m,
                parsed.Quantity ?? 0);

            var markupPrice = (parsed.Price ?? 0m) * priceMultiplier;
            product.ApplyMarkup(
                Domain.Dropshipping.Enums.DropshipMarkupType.FixedAmount,
                markupPrice - (parsed.Price ?? 0m));

            newProducts.Add(product);
            result.ImportedCount++;
        }
        catch (InvalidOperationException ex)
        {
            result.SkippedCount++;
            result.Errors.Add($"SKU {parsed.SKU}: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            result.SkippedCount++;
            result.Errors.Add($"SKU {parsed.SKU}: {ex.Message}");
        }
    }
}
