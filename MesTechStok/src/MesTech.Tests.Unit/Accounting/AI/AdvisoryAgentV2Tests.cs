using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.AI.Accounting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Accounting.AI;

/// <summary>
/// AdvisoryAgentV2 tests — daily sales advice generation.
/// Tests MESA AI integration, fallback rule-based advice, and data analysis.
/// </summary>
[Trait("Category", "Unit")]
public class AdvisoryAgentV2Tests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IProfitReportRepository> _profitRepoMock;
    private readonly Mock<ICommissionRecordRepository> _commissionRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly AdvisoryAgentV2 _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AdvisoryAgentV2Tests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        _profitRepoMock = new Mock<IProfitReportRepository>();
        _commissionRepoMock = new Mock<ICommissionRecordRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _tenantProviderMock = new Mock<ITenantProvider>();

        _tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);
        _productRepoMock.Setup(r => r.GetCountAsync()).ReturnsAsync(50);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mesa:Accounting:BaseUrl"] = "http://localhost:5101"
            })
            .Build();

        // Default: no reports, no commissions
        _profitRepoMock.Setup(r => r.GetByPeriodAsync(
                _tenantId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProfitReport>());

        _commissionRepoMock.Setup(r => r.GetByPlatformAsync(
                _tenantId, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommissionRecord>());

        _sut = new AdvisoryAgentV2(
            _httpClient,
            config,
            _profitRepoMock.Object,
            _commissionRepoMock.Object,
            _productRepoMock.Object,
            _tenantProviderMock.Object,
            new Mock<ILogger<AdvisoryAgentV2>>().Object);
    }

    private void SetupMesaAdviceResponse(HttpStatusCode status, object? responseBody = null)
    {
        var content = responseBody != null
            ? JsonSerializer.Serialize(responseBody)
            : "{}";

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    private void SetupProfitReports(string platform, decimal revenue, decimal commission, decimal cargo, decimal netProfit)
    {
        var report = ProfitReport.Create(
            _tenantId,
            DateTime.UtcNow,
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            revenue,
            totalCost: 0m,
            commission,
            cargo,
            totalTax: 0m,
            platform);

        _profitRepoMock.Setup(r => r.GetByPeriodAsync(
                _tenantId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProfitReport> { report });
    }

    private void SetupHighCommissionRecords(string platform, string category, decimal commissionRate)
    {
        var record = CommissionRecord.Create(
            _tenantId,
            platform,
            grossAmount: 1000m,
            commissionRate: commissionRate,
            commissionAmount: 1000m * commissionRate,
            serviceFee: 0m,
            orderId: "ORD-001",
            category: category);

        _commissionRepoMock.Setup(r => r.GetByPlatformAsync(
                _tenantId, platform, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommissionRecord> { record });
    }

    // ── GenerateSalesAdvice with MESA AI ──

    [Fact]
    public async Task GenerateSalesAdvice_WithMesaResponse_ReturnsRecommendations()
    {
        // Arrange
        var mesaResponse = new
        {
            recommendations = new[]
            {
                new { productName = "Urun A", platform = "Trendyol", suggestedPrice = 150m, reason = "Yuksek talep" }
            },
            warnings = new[]
            {
                new { productName = "Urun B", reason = "Dusuk marj", action = "Fiyat artir" }
            },
            platformHealth = new[]
            {
                new { platform = "Trendyol", marginTrend = "Yukselis", avgMargin = 12.5m, suggestion = "Reklam artir" }
            }
        };
        SetupMesaAdviceResponse(HttpStatusCode.OK, mesaResponse);

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert
        result.Should().NotBeNull();
        result.TopRecommendations.Should().HaveCount(1);
        result.TopRecommendations[0].ProductName.Should().Be("Urun A");
        result.Warnings.Should().HaveCount(1);
        result.PlatformHealth.Should().HaveCount(1);
    }

    // ── Fallback Rule-Based Advice ──

    [Fact]
    public async Task GenerateSalesAdvice_MesaDown_FallbackRuleBased()
    {
        // Arrange — MESA unreachable
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("MESA down"));

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert — should not throw, returns rule-based advice
        result.Should().NotBeNull();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GenerateSalesAdvice_MesaReturnsError_FallbackRuleBased()
    {
        // Arrange
        SetupMesaAdviceResponse(HttpStatusCode.InternalServerError);

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSalesAdvice_MesaReturnsNull_FallbackRuleBased()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert
        result.Should().NotBeNull();
    }

    // ── Rule-Based Negative Margin ──

    [Fact]
    public async Task GenerateSalesAdvice_NegativeMargin_WarnsProduct()
    {
        // Arrange — negative profit platform
        // NetProfit = Revenue - Cost - Commission - Cargo - Tax
        // 1000 - 0 - 800 - 300 - 0 = -100 (negative)
        var negativeReport = ProfitReport.Create(
            _tenantId,
            DateTime.UtcNow,
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            totalRevenue: 1000m,
            totalCost: 0m,
            totalCommission: 800m,
            totalCargo: 300m,
            totalTax: 0m,
            platform: "Trendyol");

        // Override: return this report for every period query
        _profitRepoMock.Setup(r => r.GetByPeriodAsync(
                _tenantId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProfitReport> { negativeReport });

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("MESA down")); // Force fallback

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert — should generate warning for negative margin
        result.Warnings.Should().NotBeEmpty();
        result.PlatformHealth.Should().Contain(p => p.MarginTrend == "Negatif");
    }

    [Fact]
    public async Task GenerateSalesAdvice_NoSalesData_ReturnsGenericRecommendation()
    {
        // Arrange — no reports
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("down"));

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert
        result.TopRecommendations.Should().NotBeEmpty();
        result.TopRecommendations.Should().Contain(r =>
            r.Reason.Contains("veri") || r.Reason.Contains("bulunamadi"));
    }

    [Fact]
    public async Task GenerateSalesAdvice_MultiplePlatforms_ComparesHealth()
    {
        // Arrange — multiple platforms with varying margins
        var reports = new List<ProfitReport>
        {
            ProfitReport.Create(_tenantId, DateTime.UtcNow, DateTime.UtcNow.ToString("yyyy-MM-dd"),
                10000m, 0m, 500m, 200m, 0m, "Trendyol"),
            ProfitReport.Create(_tenantId, DateTime.UtcNow, DateTime.UtcNow.ToString("yyyy-MM-dd"),
                5000m, 0m, 2000m, 500m, 0m, "Hepsiburada")
        };

        _profitRepoMock.Setup(r => r.GetByPeriodAsync(
                _tenantId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("down"));

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert — should have health entries for both platforms
        result.PlatformHealth.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GenerateSalesAdvice_HighMarginPlatform_RecommendsGrowth()
    {
        // Arrange — high margin scenario (>15%)
        var report = ProfitReport.Create(_tenantId, DateTime.UtcNow,
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            10000m, 0m, 500m, 200m, 0m, "Amazon");

        _profitRepoMock.Setup(r => r.GetByPeriodAsync(
                _tenantId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProfitReport> { report });

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("down"));

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert — high margin should produce recommendation
        result.PlatformHealth.Should().Contain(p => p.MarginTrend == "Yuksek");
    }

    [Fact]
    public async Task GenerateSalesAdvice_HighCommissionCategory_Warns()
    {
        // Arrange — commission rate > 20%
        SetupHighCommissionRecords("Trendyol", "Elektronik", 0.25m);

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("down"));

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert
        result.Warnings.Should().Contain(w =>
            w.ProductName.Contains("Elektronik") ||
            w.Reason.Contains("Komisyon"));
    }

    [Fact]
    public async Task GenerateSalesAdvice_TaskCanceled_FallbackRuleBased()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timeout"));

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSalesAdvice_SetsGeneratedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("down"));

        // Act
        var result = await _sut.GenerateSalesAdviceAsync(_tenantId);

        // Assert
        result.GeneratedAt.Should().BeOnOrAfter(before);
    }
}
