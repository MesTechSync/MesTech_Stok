using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Tedarikçi feed parse servisi arayüzü (dropshipping).
/// DEV 1 tarafından interface tanımlandı, DEV 3 tarafından implement edilecek.
/// </summary>
public interface IFeedParserService
{
    FeedFormat SupportedFormat { get; }
    Task<FeedParseResult> ParseAsync(Stream feedStream, FeedFieldMapping mapping, CancellationToken ct = default);
    Task<FeedValidationResult> ValidateAsync(Stream feedStream, CancellationToken ct = default);
}

public record FeedParseResult(
    IReadOnlyList<ParsedProduct> Products,
    int TotalParsed,
    int SkippedCount,
    IReadOnlyList<string> Errors);

public record ParsedProduct(
    string? SKU, string? Barcode, string? Name, string? Description,
    decimal? Price, int? Quantity, string? Category,
    string? ImageUrl, string? Brand, string? Model,
    Dictionary<string, string> ExtraFields);

public record FeedFieldMapping(
    string? SkuField, string? BarcodeField, string? NameField,
    string? PriceField, string? QuantityField, string? CategoryField,
    string? ImageField, string? DescriptionField);

public record FeedValidationResult(
    bool IsValid,
    string? Format,
    int EstimatedProductCount,
    IReadOnlyList<string> Errors);
