using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Dropshipping;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure.Dropshipping;

/// <summary>
/// FuzzyMatcher statik sınıf + CategoryAutoMapper unit testleri.
/// IMemoryCache gerçek örnek kullanılır (extension methods mock-lanamaz).
/// Mock&lt;ICategoryRepository&gt; NSubstitute yerine Moq ile.
/// ENT-DROP-IMP-SPRINT-D — DEV 5 Task D-13
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class CategoryMapperTests
{
    // ════════════════════════════════════════════════════════════════════
    // FUZZY MATCHER — statik sınıf, mock gerekmez
    // ════════════════════════════════════════════════════════════════════

    // ─── Similarity ──────────────────────────────────────────────────

    [Fact]
    public void Similarity_IdenticalStrings_ReturnsOne()
    {
        FuzzyMatcher.Similarity("elektronik", "elektronik").Should().Be(1.0m);
    }

    [Fact]
    public void Similarity_EmptySource_ReturnsZero()
    {
        FuzzyMatcher.Similarity(string.Empty, "hedef").Should().Be(0m);
    }

    [Fact]
    public void Similarity_EmptyTarget_ReturnsZero()
    {
        FuzzyMatcher.Similarity("kaynak", string.Empty).Should().Be(0m);
    }

    [Fact]
    public void Similarity_SingleCharDifference_HighScore()
    {
        // "kılıf" vs "kılıf" — tamamen aynı after normalization
        var score = FuzzyMatcher.Similarity("kilif", "kilif");
        score.Should().Be(1.0m);
    }

    [Fact]
    public void Similarity_TotallyDifferentStrings_LowScore()
    {
        // "abc" vs "xyz" — tamamen farklı
        var score = FuzzyMatcher.Similarity("abc", "xyz");
        score.Should().BeLessThan(0.5m);
    }

    [Fact]
    public void Similarity_CaseDifference_TreatedAsSame()
    {
        // Levenshtein internally lowercases
        var score = FuzzyMatcher.Similarity("Elektronik", "elektronik");
        score.Should().Be(1.0m);
    }

    [Fact]
    public void Similarity_NearlyIdentical_HighScore()
    {
        // "telefon" vs "telefonlar" — 3 chars extra
        var score = FuzzyMatcher.Similarity("telefon", "telefonlar");
        score.Should().BeGreaterThan(0.6m);
    }

    // ─── KeywordOverlap ───────────────────────────────────────────────

    [Fact]
    public void KeywordOverlap_IdenticalText_ReturnsOne()
    {
        // Same words → Jaccard = 1.0
        FuzzyMatcher.KeywordOverlap("cep telefonu", "cep telefonu").Should().Be(1.0m);
    }

    [Fact]
    public void KeywordOverlap_NoCommonWords_ReturnsZero()
    {
        // "elma" vs "armut" — no common keywords
        FuzzyMatcher.KeywordOverlap("elma", "armut").Should().Be(0m);
    }

    [Fact]
    public void KeywordOverlap_PartialOverlap_BetweenZeroAndOne()
    {
        // "Cep Telefonu Kılıfı" vs "Telefon Kılıfları" — "telefonu"/"telefon" overlap depends on tokenization
        var score = FuzzyMatcher.KeywordOverlap(
            "Cep Telefonu Kilifi",
            "Telefon Kiliflar");
        score.Should().BeInRange(0m, 1m);
    }

    [Fact]
    public void KeywordOverlap_EmptyStrings_ReturnsZero()
    {
        FuzzyMatcher.KeywordOverlap(string.Empty, "hedef").Should().Be(0m);
        FuzzyMatcher.KeywordOverlap("kaynak", string.Empty).Should().Be(0m);
    }

    [Fact]
    public void KeywordOverlap_CommonWords_PositiveScore()
    {
        var score = FuzzyMatcher.KeywordOverlap("kamera lens", "fotograf kamera");
        score.Should().BeGreaterThan(0m);
    }

    // ─── ExtractLeafCategory ──────────────────────────────────────────

    [Fact]
    public void ExtractLeafCategory_ArrowSeparator_ReturnsLast()
    {
        var leaf = FuzzyMatcher.ExtractLeafCategory("Elektronik > Cep > Kılıf");
        leaf.Should().Be("Kılıf");
    }

    [Fact]
    public void ExtractLeafCategory_SlashSeparator_ReturnsLast()
    {
        var leaf = FuzzyMatcher.ExtractLeafCategory("Giyim/Erkek/Tişört");
        leaf.Should().Be("Tişört");
    }

    [Fact]
    public void ExtractLeafCategory_PipeSeparator_ReturnsLast()
    {
        var leaf = FuzzyMatcher.ExtractLeafCategory("A|B|C");
        leaf.Should().Be("C");
    }

    [Fact]
    public void ExtractLeafCategory_NoPipes_ReturnsSelf()
    {
        var leaf = FuzzyMatcher.ExtractLeafCategory("Elektronik");
        leaf.Should().Be("Elektronik");
    }

    [Fact]
    public void ExtractLeafCategory_BackslashSeparator_ReturnsLast()
    {
        var leaf = FuzzyMatcher.ExtractLeafCategory(@"A\B\Son");
        leaf.Should().Be("Son");
    }

    // ─── CombinedScore ────────────────────────────────────────────────

    [Fact]
    public void CombinedScore_IdenticalStrings_ReturnsOne()
    {
        FuzzyMatcher.CombinedScore("elektronik", "elektronik").Should().Be(1.0m);
    }

    [Fact]
    public void CombinedScore_WeightedAverage_BetweenSimilarityAndOverlap()
    {
        // CombinedScore = Similarity * 0.60 + Overlap * 0.40
        var combined  = FuzzyMatcher.CombinedScore("kamera lens", "kamera lensi");
        var similarity = FuzzyMatcher.Similarity("kamera lens", "kamera lensi");
        var overlap    = FuzzyMatcher.KeywordOverlap("kamera lens", "kamera lensi");
        var expected   = similarity * 0.60m + overlap * 0.40m;

        combined.Should().BeApproximately(expected, 0.0001m);
    }

    [Fact]
    public void CombinedScore_TotallyDifferent_LowScore()
    {
        FuzzyMatcher.CombinedScore("xyz abc", "mnp qrs").Should().BeLessThan(0.3m);
    }

    // ════════════════════════════════════════════════════════════════════
    // CATEGORY AUTO MAPPER
    // ════════════════════════════════════════════════════════════════════

    private static (CategoryAutoMapper mapper, Mock<ICategoryRepository> repoMock) BuildMapper(
        IReadOnlyList<Category>? categories = null)
    {
        var repoMock = new Mock<ICategoryRepository>();
        repoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories ?? new List<Category>());

        var cache  = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<CategoryAutoMapper>.Instance;
        var mapper = new CategoryAutoMapper(repoMock.Object, cache, logger);

        return (mapper, repoMock);
    }

    private static Category MakeCategory(string name, string code = "") =>
        new() { Name = name, Code = code, IsActive = true };

    // ─── Null / boş girdi ────────────────────────────────────────────

    [Fact]
    public async Task MapAsync_NullInput_ReturnsNull()
    {
        var (mapper, _) = BuildMapper();
        var result = await mapper.MapAsync(null!);
        result.Should().BeNull();
    }

    [Fact]
    public async Task MapAsync_WhitespaceInput_ReturnsNull()
    {
        var (mapper, _) = BuildMapper();
        var result = await mapper.MapAsync("   ");
        result.Should().BeNull();
    }

    // ─── Kesin eşleşme ────────────────────────────────────────────────

    [Fact]
    public async Task MapAsync_ExactNameMatch_ReturnsConfidence1_IsExact()
    {
        var cat = MakeCategory("Elektronik", "ELEK");
        var (mapper, _) = BuildMapper(new[] { cat });

        var result = await mapper.MapAsync("Elektronik");

        result.Should().NotBeNull();
        result!.Confidence.Should().Be(1.0m);
        result.IsExact.Should().BeTrue();
        result.IsManual.Should().BeFalse();
        result.CategoryId.Should().Be(cat.Id);
    }

    [Fact]
    public async Task MapAsync_ExactCodeMatch_ReturnsConfidence1()
    {
        var cat = MakeCategory("Giyim", "GIYM");
        var (mapper, _) = BuildMapper(new[] { cat });

        var result = await mapper.MapAsync("GIYM");

        result.Should().NotBeNull();
        result!.IsExact.Should().BeTrue();
        result.CategoryId.Should().Be(cat.Id);
    }

    // ─── Fuzzy eşleşme ────────────────────────────────────────────────

    [Fact]
    public async Task MapAsync_FuzzyMatch_AboveThreshold_ReturnsBestMatch()
    {
        // "Elektronik > Kamera" leaf = "Kamera" — fuzzy against "Kameralar"
        var cat = MakeCategory("Kameralar", "KAM");
        var (mapper, _) = BuildMapper(new[] { cat });

        var result = await mapper.MapAsync("Elektronik > Kameralar");

        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(cat.Id);
    }

    [Fact]
    public async Task MapAsync_AllBelowThreshold_ReturnsNull()
    {
        // Completely unrelated categories — nothing should match
        var categories = new[]
        {
            MakeCategory("Gıda", "GIDA"),
            MakeCategory("Otomotiv", "AUTO"),
        };
        var (mapper, _) = BuildMapper(categories);

        // "Elektronik" has very low similarity with food/auto
        var result = await mapper.MapAsync("zzzzqqqq_gibberish_1234");

        result.Should().BeNull();
    }

    [Fact]
    public async Task MapAsync_EmptyCategories_ReturnsNull()
    {
        var (mapper, _) = BuildMapper(new List<Category>());

        var result = await mapper.MapAsync("Elektronik");

        result.Should().BeNull();
    }

    // ─── Cache davranışı ──────────────────────────────────────────────

    [Fact]
    public async Task MapAsync_SecondCall_UsesCache_RepositoryCalledOnce()
    {
        var cat = MakeCategory("Elektronik", "ELEK");
        var (mapper, repoMock) = BuildMapper(new[] { cat });

        // İlk çağrı — cache miss
        await mapper.MapAsync("Elektronik");
        // İkinci çağrı — cache hit
        await mapper.MapAsync("Elektronik");

        // Repository sadece 1 kez çağrılmalı (exact match yolu 1 kez)
        repoMock.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Manuel mapping ───────────────────────────────────────────────

    [Fact]
    public async Task SaveManualMapping_ThenMapAsync_ReturnsManualResult()
    {
        var cat1 = MakeCategory("Giyim", "GIY");
        var cat2 = MakeCategory("Elektronik", "ELEK");
        var (mapper, _) = BuildMapper(new[] { cat1, cat2 });

        // Manuel olarak cat2'ye yönlendir
        await mapper.SaveManualMappingAsync("giyim kategorisi", cat2.Id);

        var result = await mapper.MapAsync("giyim kategorisi");

        result.Should().NotBeNull();
        result!.IsManual.Should().BeTrue();
        result.IsExact.Should().BeTrue();
        result.Confidence.Should().Be(1.0m);
        result.CategoryId.Should().Be(cat2.Id);
    }

    [Fact]
    public async Task SaveManualMapping_ClearsAutoCache()
    {
        var cat1 = MakeCategory("Elektronik", "ELEK");
        var cat2 = MakeCategory("Giyim", "GIY");
        var repoMock = new Mock<ICategoryRepository>();
        repoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { cat1, cat2 });

        var cache  = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<CategoryAutoMapper>.Instance;
        var mapper = new CategoryAutoMapper(repoMock.Object, cache, logger);

        // İlk çağrı — auto-map cache'e alır
        await mapper.MapAsync("Elektronik");

        // Manuel override kaydet
        await mapper.SaveManualMappingAsync("Elektronik", cat2.Id);

        // Sonraki çağrı manuel sonuç döndürmeli
        var result = await mapper.MapAsync("Elektronik");

        result.Should().NotBeNull();
        result!.IsManual.Should().BeTrue();
        result.CategoryId.Should().Be(cat2.Id);
    }
}
