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
/// Facebook Commerce Manager catalog feed adapter.
/// Generates Facebook-compatible XML product catalog (same RSS 2.0 base as Google Merchant,
/// Facebook-specific field set without g: namespace).
/// Currency: TRY. Language: tr.
/// </summary>
public class FacebookShopFeedAdapter : ISocialFeedAdapter
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FacebookShopFeedAdapter> _logger;

    private DateTime? _lastGenerated;
    private int _lastItemCount;
    private DateTime? _nextScheduled;

    public virtual SocialFeedPlatform Platform => SocialFeedPlatform.FacebookShop;

    public FacebookShopFeedAdapter(
        AppDbContext dbContext,
        ILogger<FacebookShopFeedAdapter> logger)
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
            "[{Platform}Feed] Generating feed for store {StoreId}",
            Platform, request.StoreId);

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

            var products = await query.ToListAsync(ct);

            var errors = new List<string>();
            var items = new List<XElement>();

            foreach (var product in products)
            {
                ct.ThrowIfCancellationRequested();
                var item = BuildFacebookItem(product, request, errors);
                if (item is not null)
                    items.Add(item);
            }

            var feedXml = BuildFeedDocument(items, request);
            _ = SerializeXml(feedXml);

            var feedUrl = $"https://feeds.mestech.app/{Platform.ToString().ToLowerInvariant()}/{request.StoreId:N}.xml";

            _lastGenerated = DateTime.UtcNow;
            _lastItemCount = items.Count;

            _logger.LogInformation(
                "[{Platform}Feed] Generated {Count} items for store {StoreId} ({Errors} errors)",
                Platform, items.Count, request.StoreId, errors.Count);

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
            _logger.LogError(ex, "[{Platform}Feed] Feed generation failed for store {StoreId}", Platform, request.StoreId);
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
        _logger.LogInformation("[{Platform}Feed] Next refresh scheduled at {Next}", Platform, _nextScheduled);
        return Task.CompletedTask;
    }

    // ── Protected helpers (available to Instagram subclass) ──────────────────

    protected virtual XElement? BuildFacebookItem(Product product, FeedGenerationRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            errors.Add($"Product {product.Id}: name is empty, skipped.");
            return null;
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency;
        var availability = product.Stock > 0 ? "in stock" : "out of stock";
        var condition = "new";
        var price = product.SalePrice;

        return new XElement("item",
            new XElement("id", product.SKU),
            new XElement("title", Sanitize(product.Name, 150)),
            new XElement("description", Sanitize(product.Description ?? product.Name, 5000)),
            new XElement("availability", availability),
            new XElement("condition", condition),
            new XElement("price", $"{price.ToString("F2", CultureInfo.InvariantCulture)} {currency}"),
            new XElement("link", $"https://mestech.app/products/{product.SKU}"),
            new XElement("image_link", product.ImageUrl ?? string.Empty),
            new XElement("brand", product.Brand ?? "MesTech"));
    }

    protected static XDocument BuildFeedDocument(List<XElement> items, FeedGenerationRequest request)
    {
        var channel = new XElement("channel",
            new XElement("title", "MesTech Facebook Shop Feed"),
            new XElement("link", "https://mestech.app"),
            new XElement("description", $"Urun katalogu — {DateTime.UtcNow:yyyy-MM-dd}"),
            new XElement("language", request.Language ?? "tr"),
            items);

        var rss = new XElement("rss",
            new XAttribute("version", "2.0"),
            channel);

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), rss);
    }

    private static string SerializeXml(XDocument doc)
    {
        using var ms = new MemoryStream();
        doc.Save(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    protected static string Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var clean = value
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("&", "&amp;");
        return clean.Length > maxLength ? clean[..maxLength] : clean;
    }
}
