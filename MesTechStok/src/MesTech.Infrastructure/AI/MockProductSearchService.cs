using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI;

public class MockProductSearchService : IProductSearchService
{
    private readonly ILogger<MockProductSearchService> _logger;
    private readonly List<ProductSummary> _products;

    public MockProductSearchService(ILogger<MockProductSearchService> logger)
    {
        _logger = logger;
        _products = GenerateFakeProducts();
    }

    public Task<ProductSearchResult> SearchAsync(
        string query, Guid tenantId, int page = 1, int pageSize = 20,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] ProductSearch.Search: query={Query}, tenant={TenantId}", query, tenantId);

        var matches = _products
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || p.SKU.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || (p.Category != null && p.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Select(p => p with
            {
                Relevance = p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    ? (p.Name.Equals(query, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.7)
                    : 0.4
            })
            .OrderByDescending(p => p.Relevance)
            .ToList();

        var paged = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var result = new ProductSearchResult(paged, matches.Count, page, pageSize, DidYouMean: null);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<SimilarProduct>> FindSimilarAsync(
        Guid productId, int maxResults = 10, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] ProductSearch.FindSimilar: productId={ProductId}", productId);

        var source = _products.FirstOrDefault();
        if (source == null)
            return Task.FromResult<IReadOnlyList<SimilarProduct>>(Array.Empty<SimilarProduct>());

        var similar = _products
            .Where(p => p.ProductId != productId && p.Category == source.Category)
            .Take(maxResults)
            .Select((p, i) => new SimilarProduct(
                p.ProductId, p.SKU, p.Name, p.SalePrice,
                Similarity: 0.90 - (i * 0.05),
                Reason: "Ayni kategori"))
            .ToList();

        return Task.FromResult<IReadOnlyList<SimilarProduct>>(similar);
    }

    public Task<IReadOnlyList<ProductSummary>> DiscoverByCategoryAsync(
        string categoryName, Guid tenantId, int maxResults = 20,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] ProductSearch.DiscoverByCategory: kategori={Category}", categoryName);

        var items = _products
            .Where(p => p.Category != null &&
                        p.Category.Contains(categoryName, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductSummary>>(items);
    }

    private static List<ProductSummary> GenerateFakeProducts()
    {
        var categories = new[] { "Elektronik", "Telefon Aksesuar", "Bilgisayar", "Ev & Yasam", "Giyim" };
        var products = new List<ProductSummary>();
        var names = new[]
        {
            "Samsung Galaxy A54 Kilif", "iPhone 15 Pro Cam Ekran Koruyucu",
            "Bluetooth Kulaklik TWS Pro", "USB-C Hub 7in1 Aluminyum",
            "Mekanik Klavye RGB Cherry MX", "Kablosuz Mouse Ergonomik",
            "Laptop Stand Aluminyum Katlanir", "Webcam 4K Mikrofon",
            "Tasinabilir SSD 1TB USB-C", "Monitor Kolu Gas Spring",
            "Akilli Saat Band Silikon", "Tablet Kilif 11 inc Universal",
            "Sarj Kablosu USB-C 2m Orgu", "Powerbank 20000mAh PD 65W",
            "Kulak Ustu Kulaklik ANC", "Gaming Mousepad XXL RGB",
            "USB Flash Bellek 128GB Metal", "HDMI Kablo 4K 2m",
            "Kamera Tripod 170cm", "Ring Light 26cm Tripod",
            "Mikrofonlu Kulaklik 3.5mm", "Laptop Cantasi 15.6 inc",
            "Akilli Priz WiFi 16A", "LED Serit Isik RGB 5m",
            "Termos Bardak 500ml Celik", "Yoga Mati 6mm TPE",
            "Spor Bileklik Silikon", "Mutfak Tartisi Dijital",
            "Kitap Okuma Lambasi LED", "Masa Ustu Organizator Bambu",
            "Cep Telefonu Tutucu Arac", "Bluetooth Hoparlor Tasinabilir",
            "Dijital Fotograf Cercevesi 10inc", "Akilli Ampul E27 RGB WiFi",
            "Hava Nemlendirici USB Mini", "Elektrikli Dis Fircasi Sarj",
            "Masa Ustu Fan USB Metal", "Pil Sarj Cihazi AA/AAA",
            "Gecirmez Telefon Kilifi", "Stylus Kalem Tablet Uyumlu",
            "Kablosuz Sarj Pad 15W Qi", "Arac Kokusu Havalandirma",
            "Bez Canta Tote Bag Kanvas", "Termal Yazici Etiket 80mm",
            "Barkod Okuyucu USB Kablolu", "Para Kasasi Celik Kucuk",
            "Etiket Makinesi P-Touch", "Kartvizitlik Deri 200 Adet",
            "Dosya Dolabi Metal 4 Cekmece", "Beyaz Tahta 120x90 Manyetik"
        };

        for (int i = 0; i < names.Length; i++)
        {
            products.Add(new ProductSummary(
                Guid.NewGuid(),
                $"SKU-SRCH-{i + 1:D3}",
                names[i],
                categories[i % categories.Length],
                SalePrice: 29.90m + (i * 17.5m),
                StockQuantity: 10 + (i * 5),
                Relevance: 0.0));
        }

        return products;
    }
}
