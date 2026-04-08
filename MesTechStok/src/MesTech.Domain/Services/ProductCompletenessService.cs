using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// Ürün tamamlılık skoru hesaplama servisi.
/// Platform bazlı hazırlık puanı (0-100%).
/// Eksik alanlar listeyle döner — hangi bilgilerin girilmesi gerektiğini gösterir.
///
/// Trendyol zorunlu alanlar: Title, Barcode, Category, Brand, Image, Price, Stock
/// Hepsiburada: Title, Barcode, Category, Image, Price
/// Amazon: Title, EAN/UPC, Category, Brand, Image, BulletPoints (5), Description
/// </summary>
public sealed class ProductCompletenessService
{
    public ProductCompletenessResult Calculate(Product product, PlatformType? platform = null)
    {
        ArgumentNullException.ThrowIfNull(product);

        var checks = new List<CompletenessCheck>
        {
            new("Name", !string.IsNullOrWhiteSpace(product.Name), 15),
            new("SKU", !string.IsNullOrWhiteSpace(product.SKU), 10),
            new("Barcode", !string.IsNullOrWhiteSpace(product.Barcode), 10),
            new("Description", !string.IsNullOrWhiteSpace(product.Description), 10),
            new("Category", product.CategoryId != Guid.Empty, 10),
            new("SalePrice", product.SalePrice > 0, 15),
            new("PurchasePrice", product.PurchasePrice > 0, 5),
            new("Stock", product.Stock >= 0, 5),
            new("Brand", !string.IsNullOrWhiteSpace(product.Brand), 5),
            new("Weight", product.Weight.HasValue && product.Weight > 0, 5),
            new("TaxRate", product.TaxRate >= 0, 5),
            new("IsActive", product.IsActive, 5),
        };

        var totalWeight = checks.Sum(c => c.Weight);
        var earnedWeight = checks.Where(c => c.IsMet).Sum(c => c.Weight);
        var score = totalWeight > 0 ? Math.Round((decimal)earnedWeight / totalWeight * 100, 1) : 0;
        var missingFields = checks.Where(c => !c.IsMet).Select(c => c.FieldName).ToList();

        return new ProductCompletenessResult(
            Score: score,
            TotalChecks: checks.Count,
            PassedChecks: checks.Count(c => c.IsMet),
            MissingFields: missingFields.AsReadOnly());
    }
}

public record CompletenessCheck(string FieldName, bool IsMet, int Weight);

public record ProductCompletenessResult(
    decimal Score,
    int TotalChecks,
    int PassedChecks,
    IReadOnlyList<string> MissingFields);
