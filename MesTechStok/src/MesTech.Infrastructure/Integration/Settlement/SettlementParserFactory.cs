using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Infrastructure.Integration.Settlement;

/// <summary>
/// Factory for resolving platform-specific settlement parsers.
/// Resolves by platform name (case-insensitive).
/// Supports: Trendyol, Amazon, AmazonEu, Hepsiburada, Ciceksepeti, N11, Pazarama, OpenCart,
/// eBay, Ozon, PttAVM, Shopify, Etsy, WooCommerce, Zalando (15 platform).
/// </summary>
public sealed class SettlementParserFactory : ISettlementParserFactory
{
    private readonly Dictionary<string, ISettlementParser> _parsers;

    public SettlementParserFactory(IEnumerable<ISettlementParser> parsers)
    {
        ArgumentNullException.ThrowIfNull(parsers);

        _parsers = new Dictionary<string, ISettlementParser>(StringComparer.OrdinalIgnoreCase);

        foreach (var parser in parsers)
        {
            _parsers[parser.Platform] = parser;
        }
    }

    public ISettlementParser GetParser(string platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);

        if (_parsers.TryGetValue(platform, out var parser))
            return parser;

        var supported = string.Join(", ", _parsers.Keys.OrderBy(k => k));
        throw new ArgumentException(
            $"Unsupported settlement platform: '{platform}'. Supported platforms: {supported}",
            nameof(platform));
    }

    public IReadOnlyList<string> SupportedPlatforms
        => _parsers.Keys.OrderBy(k => k).ToList().AsReadOnly();
}
