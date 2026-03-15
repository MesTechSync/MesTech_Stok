using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Accounting;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Services;

/// <summary>
/// PlatformCommissionRateProvider unit testleri — settlement verilerinden dinamik oran turetme.
/// 18 test: happy path, edge case, hata durumlari, cache TTL, multi-platform.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "CommissionRate")]
[Trait("Phase", "Dalga9")]
public class PlatformCommissionRateProviderTests
{
    private readonly Mock<ISettlementBatchRepository> _repoMock;
    private readonly Mock<ILogger<PlatformCommissionRateProvider>> _loggerMock;
    private readonly PlatformCommissionRateProvider _sut;

    public PlatformCommissionRateProviderTests()
    {
        _repoMock = new Mock<ISettlementBatchRepository>();
        _loggerMock = new Mock<ILogger<PlatformCommissionRateProvider>>();
        _sut = new PlatformCommissionRateProvider(_repoMock.Object, _loggerMock.Object);
    }

    private static SettlementBatch CreateBatch(
        string platform, decimal totalGross, decimal totalCommission,
        DateTime? periodEnd = null)
    {
        return SettlementBatch.Create(
            tenantId: Guid.Empty,
            platform: platform,
            periodStart: (periodEnd ?? DateTime.UtcNow).AddDays(-7),
            periodEnd: periodEnd ?? DateTime.UtcNow,
            totalGross: totalGross,
            totalCommission: totalCommission,
            totalNet: totalGross - totalCommission);
    }

    // ═══════════════════════════════════════════════════════════
    // HAPPY PATH
    // ═══════════════════════════════════════════════════════════

    // Test 1: Trendyol — settlement batch varsa gercek oran donmeli
    [Fact]
    public async Task GetRateAsync_TrendyolWithSettlement_ReturnsDynamicRate()
    {
        var batch = CreateBatch("Trendyol", 10000m, 1200m);
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        var result = await _sut.GetRateAsync("Trendyol", null);

        result.Should().NotBeNull();
        result!.Rate.Should().Be(0.12m); // 1200/10000 = 0.12
        result.Source.Should().Be("TrendyolSettlement");
        result.Type.Should().Be(CommissionType.Percentage);
    }

    // Test 2: Hepsiburada — farkli oran
    [Fact]
    public async Task GetRateAsync_HepsiburadaWithSettlement_ReturnsDynamicRate()
    {
        var batch = CreateBatch("Hepsiburada", 5000m, 900m);
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Hepsiburada", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        var result = await _sut.GetRateAsync("Hepsiburada", null);

        result.Should().NotBeNull();
        result!.Rate.Should().Be(0.18m); // 900/5000 = 0.18
        result.Source.Should().Be("HepsiburadaSettlement");
    }

    // Test 3: Amazon — farkli oran
    [Fact]
    public async Task GetRateAsync_AmazonWithSettlement_ReturnsDynamicRate()
    {
        var batch = CreateBatch("Amazon", 20000m, 3000m);
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Amazon", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        var result = await _sut.GetRateAsync("Amazon", null);

        result.Should().NotBeNull();
        result!.Rate.Should().Be(0.15m); // 3000/20000 = 0.15
    }

    // Test 4: CachedUntil 6 saat sonra olmali
    [Fact]
    public async Task GetRateAsync_ValidResult_CachedUntilIs6HoursFromNow()
    {
        var batch = CreateBatch("Trendyol", 1000m, 150m);
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        var before = DateTime.UtcNow.AddHours(5.9);
        var result = await _sut.GetRateAsync("Trendyol", null);
        var after = DateTime.UtcNow.AddHours(6.1);

        result.Should().NotBeNull();
        result!.CachedUntil.Should().BeAfter(before);
        result.CachedUntil.Should().BeBefore(after);
    }

    // ═══════════════════════════════════════════════════════════
    // NULL / FALLBACK
    // ═══════════════════════════════════════════════════════════

    // Test 5: Settlement yok — null donmeli (fallback tetiklenir)
    [Fact]
    public async Task GetRateAsync_NoSettlement_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Pazarama", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());

        var result = await _sut.GetRateAsync("Pazarama", null);

        result.Should().BeNull();
    }

    // Test 6: Bos liste — null donmeli
    [Fact]
    public async Task GetRateAsync_EmptyBatchList_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "N11", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SettlementBatch>)new List<SettlementBatch>());

        var result = await _sut.GetRateAsync("N11", null);

        result.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    // EDGE CASES
    // ═══════════════════════════════════════════════════════════

    // Test 7: GrossAmount sifir — null donmeli (bolme hatasi onlenir)
    [Fact]
    public async Task GetRateAsync_ZeroGrossAmount_ReturnsNull()
    {
        var batch = CreateBatch("Trendyol", 0m, 0m);
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        var result = await _sut.GetRateAsync("Trendyol", null);

        result.Should().BeNull();
    }

    // Test 8: Negatif GrossAmount — null donmeli
    [Fact]
    public async Task GetRateAsync_NegativeGrossAmount_ReturnsNull()
    {
        var batch = CreateBatch("Hepsiburada", -100m, -15m);
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Hepsiburada", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        var result = await _sut.GetRateAsync("Hepsiburada", null);

        result.Should().BeNull();
    }

    // Test 9: Oran %50'den buyuk — mantik disi, null donmeli
    [Fact]
    public async Task GetRateAsync_RateAbove50Percent_ReturnsNull()
    {
        var batch = CreateBatch("Ciceksepeti", 100m, 60m); // %60 — mantik disi
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Ciceksepeti", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        var result = await _sut.GetRateAsync("Ciceksepeti", null);

        result.Should().BeNull();
    }

    // Test 10: Komisyon sifir — %0 gecerli (promosyon)
    [Fact]
    public async Task GetRateAsync_ZeroCommission_ReturnsZeroRate()
    {
        var batch = CreateBatch("Pazarama", 5000m, 0m);
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Pazarama", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        var result = await _sut.GetRateAsync("Pazarama", null);

        result.Should().NotBeNull();
        result!.Rate.Should().Be(0m);
    }

    // ═══════════════════════════════════════════════════════════
    // MULTI-BATCH (en son batch secimi)
    // ═══════════════════════════════════════════════════════════

    // Test 11: Birden fazla batch — en son tarihli secilmeli
    [Fact]
    public async Task GetRateAsync_MultipleBatches_SelectsLatestByPeriodEnd()
    {
        var oldBatch = CreateBatch("Trendyol", 10000m, 1500m, DateTime.UtcNow.AddDays(-30)); // %15
        var newBatch = CreateBatch("Trendyol", 10000m, 1000m, DateTime.UtcNow.AddDays(-1));  // %10

        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { oldBatch, newBatch });

        var result = await _sut.GetRateAsync("Trendyol", null);

        result.Should().NotBeNull();
        result!.Rate.Should().Be(0.10m, "en son batch secilmeli");
    }

    // Test 12: Ters sirada verilen batch'ler — yine en son secilmeli
    [Fact]
    public async Task GetRateAsync_BatchesInReverseOrder_StillSelectsLatest()
    {
        var newBatch = CreateBatch("Amazon", 8000m, 960m, DateTime.UtcNow.AddDays(-2));  // %12
        var oldBatch = CreateBatch("Amazon", 8000m, 1600m, DateTime.UtcNow.AddDays(-60)); // %20

        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), "Amazon", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { newBatch, oldBatch });

        var result = await _sut.GetRateAsync("Amazon", null);

        result.Should().NotBeNull();
        result!.Rate.Should().Be(0.12m);
    }

    // ═══════════════════════════════════════════════════════════
    // HATA DURUMLARI
    // ═══════════════════════════════════════════════════════════

    // Test 13: Repository exception — null donmeli (fallback tetiklenir)
    [Fact]
    public async Task GetRateAsync_RepositoryThrows_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB baglanti hatasi"));

        var result = await _sut.GetRateAsync("Trendyol", null);

        result.Should().BeNull();
    }

    // Test 14: Timeout exception — null donmeli
    [Fact]
    public async Task GetRateAsync_RepositoryTimesOut_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Query suresi asildi"));

        var result = await _sut.GetRateAsync("Hepsiburada", null);

        result.Should().BeNull();
    }

    // Test 15: OperationCanceledException — propagate etmeli (catch etmemeli)
    [Fact]
    public async Task GetRateAsync_Cancelled_PropagatesException()
    {
        _repoMock.Setup(r => r.GetByPlatformAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = () => _sut.GetRateAsync("Trendyol", null);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════

    // Test 16: Null platform — ArgumentException
    [Fact]
    public async Task GetRateAsync_NullPlatform_ThrowsArgumentException()
    {
        var act = () => _sut.GetRateAsync(null!, null);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // Test 17: Empty platform — ArgumentException
    [Fact]
    public async Task GetRateAsync_EmptyPlatform_ThrowsArgumentException()
    {
        var act = () => _sut.GetRateAsync("", null);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // Test 18: Constructor null repo — ArgumentNullException
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new PlatformCommissionRateProvider(null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settlementRepo");
    }
}
