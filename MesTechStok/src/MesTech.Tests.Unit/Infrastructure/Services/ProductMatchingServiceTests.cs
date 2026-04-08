using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Sprint 3 — ProductMatchingService testleri.
/// EAN match (%100), SKU match (%80), no match (%0), batch.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class ProductMatchingServiceTests
{
    private readonly Mock<IProductRepository> _repo = new();
    private readonly ProductMatchingService _sut;

    public ProductMatchingServiceTests()
    {
        _sut = new ProductMatchingService(_repo.Object, Mock.Of<ILogger<ProductMatchingService>>());
    }

    private static Product MakeProduct(string sku = "SKU-001", string? barcode = "8690001000011") =>
        new() { SKU = sku, Name = $"Product {sku}", Barcode = barcode,
                CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid() };

    // ══════════════════════════════════════
    // Kural 1: Barcode → %100 güven
    // ══════════════════════════════════════

    [Fact]
    public async Task Match_ByBarcode_ShouldReturn100Confidence()
    {
        var product = MakeProduct();
        _repo.Setup(r => r.GetByBarcodeAsync("8690001000011", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.MatchAsync("8690001000011", null, null);

        result.Confidence.Should().Be(100);
        result.MatchedBy.Should().Be(MatchStrategy.Barcode);
        result.IsAutoMatch.Should().BeTrue();
        result.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task Match_ByBarcode_NotFound_ShouldFallThrough()
    {
        _repo.Setup(r => r.GetByBarcodeAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.MatchAsync("UNKNOWN", null, null);

        result.Confidence.Should().Be(0);
        result.MatchedBy.Should().Be(MatchStrategy.None);
    }

    // ══════════════════════════════════════
    // Kural 2: SKU → %80 güven
    // ══════════════════════════════════════

    [Fact]
    public async Task Match_BySKU_ShouldReturn80Confidence()
    {
        var product = MakeProduct("TY-001", null);
        _repo.Setup(r => r.GetBySKUAsync("TY-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.MatchAsync(null, "TY-001", null);

        result.Confidence.Should().Be(80);
        result.MatchedBy.Should().Be(MatchStrategy.SKU);
        result.IsAutoMatch.Should().BeFalse("SKU match requires manual confirmation");
    }

    [Fact]
    public async Task Match_BarcodeFirst_ThenSKU()
    {
        // Barcode eşleşirse SKU'ya bakmaz
        var product = MakeProduct("TY-001", "8690001000011");
        _repo.Setup(r => r.GetByBarcodeAsync("8690001000011", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.MatchAsync("8690001000011", "TY-001", null);

        result.MatchedBy.Should().Be(MatchStrategy.Barcode, "barcode takes priority over SKU");
        result.Confidence.Should().Be(100);
    }

    // ══════════════════════════════════════
    // No match
    // ══════════════════════════════════════

    [Fact]
    public async Task Match_NothingProvided_ShouldReturnZero()
    {
        var result = await _sut.MatchAsync(null, null, null);

        result.Confidence.Should().Be(0);
        result.MatchedBy.Should().Be(MatchStrategy.None);
        result.IsAutoMatch.Should().BeFalse();
        result.ProductId.Should().BeNull();
    }

    [Fact]
    public async Task Match_AllMiss_ShouldReturnZero()
    {
        _repo.Setup(r => r.GetByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _repo.Setup(r => r.GetBySKUAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.MatchAsync("MISSING-BC", "MISSING-SKU", "Short");

        result.Confidence.Should().Be(0);
    }

    // ══════════════════════════════════════
    // Batch
    // ══════════════════════════════════════

    [Fact]
    public async Task BatchMatch_ShouldReturnResultPerRequest()
    {
        var p1 = MakeProduct("SKU-A", "111");
        _repo.Setup(r => r.GetByBarcodeAsync("111", It.IsAny<CancellationToken>())).ReturnsAsync(p1);
        _repo.Setup(r => r.GetByBarcodeAsync("222", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _repo.Setup(r => r.GetBySKUAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var requests = new List<ProductMatchRequest>
        {
            new() { Barcode = "111", SKU = "SKU-A" },
            new() { Barcode = "222", SKU = "SKU-B" }
        };

        var results = await _sut.BatchMatchAsync(requests);

        results.Should().HaveCount(2);
        results[0].Confidence.Should().Be(100);
        results[1].Confidence.Should().Be(0);
    }

    [Fact]
    public async Task BatchMatch_ShouldSetRequestFields()
    {
        _repo.Setup(r => r.GetByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _repo.Setup(r => r.GetBySKUAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var requests = new List<ProductMatchRequest>
        {
            new() { Barcode = "BC-1", SKU = "SK-1" }
        };

        var results = await _sut.BatchMatchAsync(requests);

        results[0].RequestBarcode.Should().Be("BC-1");
        results[0].RequestSKU.Should().Be("SK-1");
    }

    [Fact]
    public async Task BatchMatch_Empty_ShouldReturnEmpty()
    {
        var results = await _sut.BatchMatchAsync(new List<ProductMatchRequest>());
        results.Should().BeEmpty();
    }
}
