using System.Globalization;
using System.Text;
using System.Xml.Linq;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Feed;

/// <summary>
/// Google Merchant Center RSS 2.0 feed adapter.
/// Generates Google Shopping XML with g: namespace (Atom/RSS hybrid).
/// Currency: TRY. Language: tr.
/// </summary>
public class GoogleMerchantFeedAdapter : ISocialFeedAdapter
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<GoogleMerchantFeedAdapter> _logger;

    private static readonly XNamespace G = "http://base.google.com/ns/1.0";
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    private DateTime? _lastGenerated;
    private int _lastItemCount;
    private DateTime? _nextScheduled;

    public SocialFeedPlatform Platform => SocialFeedPlatform.GoogleMerchant;

    public GoogleMerchantFeedAdapter(
        AppDbContext dbContext,
        ILogger<GoogleMerchantFeedAdapter> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FeedGenerationResult> GenerateFeedAsync(
        FeedGenerationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "[GoogleMerchantFeed] Generating feed for store {StoreId} currency={Currency} lang={Language}",
            request.StoreId, request.Currency, request.Language);

        try
        {
            var query = _dbContext.Products
                .Where(p => p.TenantId == request.StoreId && p.IsActive && !p.IsDeleted);

            if (request.CategoryFilter is { Count: > 0 })
            {
                query = query.Where(p =>
                    request.CategoryFilter.Contains(p.Brand ?? string.Empty) ||
                    p.Tags != null && request.CategoryFilter.Any(f => p.Tags.Contains(f)));
            }

            var products = await query.ToListAsync(ct).ConfigureAwait(false);

            var errors = new List<string>();
            var items = new List<XElement>();

            foreach (var product in products)
            {
                ct.ThrowIfCancellationRequested();
                var item = BuildGoogleItem(product, request, errors);
                if (item is not null)
                    items.Add(item);
            }

            var feedXml = BuildFeedDocument(items, request);
            var feedContent = SerializeXml(feedXml);

            // In a real deployment the feed would be written to object storage (MinIO/S3)
            // and the URL returned. Here we return a placeholder URL pattern.
            var feedUrl = $"https://feeds.mestech.app/google-merchant/{request.StoreId:N}.xml";

            _lastGenerated = DateTime.UtcNow;
            _lastItemCount = items.Count;

            _logger.LogInformation(
                "[GoogleMerchantFeed] Generated {Count} items for store {StoreId} ({Errors} errors)",
                items.Count, request.StoreId, errors.Count);

            return new FeedGenerationResult(
                Success: true,
                FeedUrl: feedUrl,
                ItemCount: items.Count,
                GeneratedAt: _lastGenerated.Value,
                Errors: errors.Count > 0 ? errors.AsReadOnly() : null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GoogleMerchantFeed] Feed generation failed for store {StoreId}", request.StoreId);
            return new FeedGenerationResult(
                Success: false,
                FeedUrl: null,
                ItemCount: 0,
                GeneratedAt: DateTime.UtcNow,
                Errors: new[] { ex.Message });
        }
    }

    public Task<SocialFeedValidationResult> ValidateFeedAsync(string feedUrl, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(feedUrl))
            errors.Add("Feed URL bos olamaz.");
        else if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out _))
            errors.Add($"Gecersiz feed URL: {feedUrl}");

        return Task.FromResult(new SocialFeedValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors.AsReadOnly(),
            Warnings: warnings.AsReadOnly()));
    }

    public Task<FeedStatus> GetFeedStatusAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new FeedStatus(
            LastGenerated: _lastGenerated,
            ItemCount: _lastItemCount,
            NextScheduled: _nextScheduled,
            IsHealthy: _lastGenerated.HasValue));
    }

    public Task ScheduleRefreshAsync(TimeSpan interval, CancellationToken ct = default)
    {
        _nextScheduled = (_lastGenerated ?? DateTime.UtcNow).Add(interval);
        _logger.LogInformation("[GoogleMerchantFeed] Next refresh scheduled at {Next}", _nextScheduled);
        return Task.CompletedTask;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private XElement? BuildGoogleItem(Product product, FeedGenerationRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            errors.Add($"Product {product.Id}: name is empty, skipped.");
            return null;
        }

        var price = product.SalePrice;
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency;
        var availability = product.Stock > 0 ? "in stock" : "out of stock";
        var condition = "new";

        var item = new XElement("item",
            new XElement(G + "id", product.SKU),
            new XElement(G + "title", Sanitize(product.Name, 150)),
            new XElement(G + "description", Sanitize(product.Description ?? product.Name, 5000)),
            new XElement(G + "link", BuildProductUrl(product)),
            new XElement(G + "image_link", product.ImageUrl ?? string.Empty),
            new XElement(G + "availability", availability),
            new XElement(G + "condition", condition),
            new XElement(G + "price", $"{price.ToString("F2", CultureInfo.InvariantCulture)} {currency}"),
            new XElement(G + "brand", product.Brand ?? "MesTech"));

        // Optional: sale_price when discounted
        if (product.DiscountedPrice.HasValue && product.DiscountedPrice.Value < price)
        {
            item.Add(new XElement(G + "sale_price",
                $"{product.DiscountedPrice.Value.ToString("F2", CultureInfo.InvariantCulture)} {currency}"));
        }

        // GTIN: barcode, MPN: SKU
        if (!string.IsNullOrWhiteSpace(product.Barcode))
            item.Add(new XElement(G + "gtin", product.Barcode));
        else
            item.Add(new XElement(G + "mpn", product.SKU));

        // google_product_category — default "Diger" if not mapped
        item.Add(new XElement(G + "google_product_category", "Diger"));

        return item;
    }

    private static XDocument BuildFeedDocument(List<XElement> items, FeedGenerationRequest request)
    {
        var channel = new XElement("channel",
            new XElement("title", "MesTech Google Merchant Feed"),
            new XElement("link", "https://mestech.app"),
            new XElement("description", $"Urun katalogu — {DateTime.UtcNow:yyyy-MM-dd}"),
            new XElement("language", request.Language ?? "tr"),
            items);

        var rss = new XElement("rss",
            new XAttribute("version", "2.0"),
            new XAttribute(XNamespace.Xmlns + "g", G),
            new XAttribute(XNamespace.Xmlns + "atom", Atom),
            channel);

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), rss);
    }

    private static string SerializeXml(XDocument doc)
    {
        using var ms = new MemoryStream();
        doc.Save(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string BuildProductUrl(Product product)
        => $"https://mestech.app/products/{product.SKU}";

    private static string Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var clean = value
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("&", "&amp;");
        return clean.Length > maxLength ? clean[..maxLength] : clean;
    }
}
