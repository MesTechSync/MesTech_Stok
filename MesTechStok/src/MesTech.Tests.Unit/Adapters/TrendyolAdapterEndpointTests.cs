using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Platform;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

/// <summary>
/// DEV 5 — Dalga 10: TrendyolAdapter 12 extended endpoint tests.
/// 3 tests per endpoint: happy path, auth error (401), API error (500).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Trendyol")]
public class TrendyolAdapterEndpointTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly TrendyolAdapter _sut;
    private readonly JsonSerializerOptions _jsonOptions;

    public TrendyolAdapterEndpointTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://apigw.trendyol.com")
        };
        var logger = NullLogger<TrendyolAdapter>.Instance;
        var options = Options.Create(new TrendyolOptions());
        _sut = new TrendyolAdapter(_httpClient, logger, options);

        // Configure auth so EnsureConfigured passes
        var credentials = new Dictionary<string, string>
        {
            ["ApiKey"] = "test-api-key",
            ["ApiSecret"] = "test-api-secret",
            ["SupplierId"] = "123456"
        };

        // Setup successful connection test response to configure the adapter
        SetupResponse(HttpStatusCode.OK, "{\"totalElements\":1}");
        _sut.TestConnectionAsync(credentials).GetAwaiter().GetResult();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    // ═══════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════

    private void SetupResponse(HttpStatusCode statusCode, string content)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    private void SetupResponseForUrl(HttpStatusCode statusCode, string content, string urlContains)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    // ═══════════════════════════════════════════
    // 1. ArchiveProductsAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ArchiveProductsAsync_HappyPath_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");
        var barcodes = new List<string> { "8680001234567", "8680009876543" };

        var result = await _sut.ArchiveProductsAsync(barcodes);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ArchiveProductsAsync_AuthError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");
        var barcodes = new List<string> { "8680001234567" };

        var result = await _sut.ArchiveProductsAsync(barcodes);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveProductsAsync_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");
        var barcodes = new List<string> { "8680001234567" };

        var result = await _sut.ArchiveProductsAsync(barcodes);

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // 2. UnlockProductsAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task UnlockProductsAsync_HappyPath_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");
        var barcodes = new List<string> { "8680001234567" };

        var result = await _sut.UnlockProductsAsync(barcodes);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnlockProductsAsync_AuthError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");
        var barcodes = new List<string> { "8680001234567" };

        var result = await _sut.UnlockProductsAsync(barcodes);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockProductsAsync_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");
        var barcodes = new List<string> { "8680001234567" };

        var result = await _sut.UnlockProductsAsync(barcodes);

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // 3. GetQuestionsAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task GetQuestionsAsync_HappyPath_ReturnsQuestions()
    {
        var epoch = new DateTimeOffset(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var responseJson = JsonSerializer.Serialize(new
        {
            content = new[]
            {
                new { id = 1001L, text = "Bu urun su gecirmez mi?", productId = 555L, status = "WAITING_FOR_ANSWER", creationDate = epoch },
                new { id = 1002L, text = "Renk secenekleri var mi?", productId = 556L, status = "ANSWERED", creationDate = epoch }
            },
            totalElements = 2,
            totalPages = 1
        }, _jsonOptions);

        SetupResponse(HttpStatusCode.OK, responseJson);

        var result = await _sut.GetQuestionsAsync(0, 10);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1001);
        result[0].Text.Should().Be("Bu urun su gecirmez mi?");
        result[0].ProductId.Should().Be(555);
        result[0].Status.Should().Be("WAITING_FOR_ANSWER");
        result[1].Id.Should().Be(1002);
    }

    [Fact]
    public async Task GetQuestionsAsync_AuthError_ReturnsEmpty()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.GetQuestionsAsync(0, 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuestionsAsync_ServerError_ReturnsEmpty()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.GetQuestionsAsync(0, 10);

        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // 4. AnswerQuestionAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task AnswerQuestionAsync_HappyPath_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.AnswerQuestionAsync(1001, "Evet, su gecirmezdir.");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnswerQuestionAsync_AuthError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.AnswerQuestionAsync(1001, "Evet");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnswerQuestionAsync_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.AnswerQuestionAsync(1001, "Evet");

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // 5. GetClaimsAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task GetClaimsAsync_HappyPath_ReturnsClaims()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            content = new[]
            {
                new { id = 5001L, orderId = 9001L, reason = "DAMAGED", status = "OPEN", amount = 125.50m },
                new { id = 5002L, orderId = 9002L, reason = "WRONG_ITEM", status = "CLOSED", amount = 99.90m }
            },
            totalElements = 2,
            totalPages = 1
        }, _jsonOptions);

        SetupResponse(HttpStatusCode.OK, responseJson);

        var result = await _sut.GetClaimsAsync(null, null);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(5001);
        result[0].OrderId.Should().Be(9001);
        result[0].Reason.Should().Be("DAMAGED");
        result[0].Amount.Should().Be(125.50m);
        result[1].Status.Should().Be("CLOSED");
    }

    [Fact]
    public async Task GetClaimsAsync_AuthError_ReturnsEmpty()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.GetClaimsAsync(null, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClaimsAsync_ServerError_ReturnsEmpty()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.GetClaimsAsync(null, null);

        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // 6. ApproveClaimByIdAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ApproveClaimByIdAsync_HappyPath_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.ApproveClaimByIdAsync(5001);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveClaimByIdAsync_AuthError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.ApproveClaimByIdAsync(5001);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveClaimByIdAsync_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.ApproveClaimByIdAsync(5001);

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // 7. RejectClaimByIdAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task RejectClaimByIdAsync_HappyPath_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.RejectClaimByIdAsync(5002, "Urun hasar gormemis.");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RejectClaimByIdAsync_AuthError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.RejectClaimByIdAsync(5002, "Reddedildi");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RejectClaimByIdAsync_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.RejectClaimByIdAsync(5002, "Reddedildi");

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // 8. SendInvoiceAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task SendInvoiceAsync_HappyPath_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.SendInvoiceAsync(9001, "INV-2026-001", new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendInvoiceAsync_AuthError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.SendInvoiceAsync(9001, "INV-2026-001", DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendInvoiceAsync_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.SendInvoiceAsync(9001, "INV-2026-001", DateTime.UtcNow);

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // 9. GetSettlementsAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task GetSettlementsAsync_HappyPath_ReturnsSettlements()
    {
        var epoch = new DateTimeOffset(new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var responseJson = JsonSerializer.Serialize(new
        {
            content = new[]
            {
                new { id = 7001L, amount = 5000.00m, currency = "TRY", status = "COMPLETED", date = epoch },
                new { id = 7002L, amount = 3200.50m, currency = "TRY", status = "PENDING", date = epoch }
            },
            totalElements = 2,
            totalPages = 1
        }, _jsonOptions);

        SetupResponse(HttpStatusCode.OK, responseJson);

        var result = await _sut.GetSettlementsAsync(
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(7001);
        result[0].Amount.Should().Be(5000.00m);
        result[0].Currency.Should().Be("TRY");
        result[0].Status.Should().Be("COMPLETED");
        result[1].Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task GetSettlementsAsync_AuthError_ReturnsEmpty()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.GetSettlementsAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSettlementsAsync_ServerError_ReturnsEmpty()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.GetSettlementsAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // 10. SplitPackageAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task SplitPackageAsync_HappyPath_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.SplitPackageAsync(8001, new List<long> { 100, 101, 102 });

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SplitPackageAsync_AuthError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.SplitPackageAsync(8001, new List<long> { 100 });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SplitPackageAsync_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.SplitPackageAsync(8001, new List<long> { 100 });

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // 11. UpdateBoxInfoAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task UpdateBoxInfoAsync_HappyPath_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.UpdateBoxInfoAsync(8001, 5, 2);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBoxInfoAsync_AuthError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.UpdateBoxInfoAsync(8001, 5, 2);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBoxInfoAsync_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.UpdateBoxInfoAsync(8001, 5, 2);

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // 12. GetCompensationsAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task GetCompensationsAsync_HappyPath_ReturnsCompensations()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            content = new[]
            {
                new { id = 3001L, claimId = 5001L, amount = 150.00m, status = "APPROVED" },
                new { id = 3002L, claimId = 5002L, amount = 75.50m, status = "PENDING" }
            },
            totalElements = 2,
            totalPages = 1
        }, _jsonOptions);

        SetupResponse(HttpStatusCode.OK, responseJson);

        var result = await _sut.GetCompensationsAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3001);
        result[0].ClaimId.Should().Be(5001);
        result[0].Amount.Should().Be(150.00m);
        result[0].Status.Should().Be("APPROVED");
        result[1].Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task GetCompensationsAsync_AuthError_ReturnsEmpty()
    {
        SetupResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        var result = await _sut.GetCompensationsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCompensationsAsync_ServerError_ReturnsEmpty()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Internal Server Error\"}");

        var result = await _sut.GetCompensationsAsync();

        result.Should().BeEmpty();
    }
}
