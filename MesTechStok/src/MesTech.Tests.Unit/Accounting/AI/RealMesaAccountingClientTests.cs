using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Infrastructure.AI.Accounting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Accounting.AI;

/// <summary>
/// RealMesaAccountingClient tests — Classify, Extract, SuggestReconciliation with mock HTTP + fallback.
/// </summary>
[Trait("Category", "Unit")]
public class RealMesaAccountingClientTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly MockMesaAccountingService _mockFallback;
    private readonly RealMesaAccountingClient _sut;

    public RealMesaAccountingClientTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpHandlerMock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mesa:Accounting:BaseUrl"] = "http://localhost:5101",
                ["Mesa:Accounting:UseMock"] = "true"
            })
            .Build();

        _mockFallback = new MockMesaAccountingService(
            config,
            new Mock<ILogger<MockMesaAccountingService>>().Object);

        _sut = new RealMesaAccountingClient(
            _httpClient,
            config,
            _mockFallback,
            new Mock<ILogger<RealMesaAccountingClient>>().Object);
    }

    private void SetupHttpResponse(HttpStatusCode status, string content)
    {
        _httpHandlerMock
            .Protected()
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

    private void SetupHttpException()
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("MESA OS unreachable"));
    }

    // ── ClassifyDocumentAsync ───────────────────────────────────────

    [Fact]
    public async Task ClassifyDocument_Success_ReturnsClassification()
    {
        var response = JsonSerializer.Serialize(new
        {
            type = "Invoice",
            confidence = 0.92m,
            rawText = "FATURA TEST"
        });

        SetupHttpResponse(HttpStatusCode.OK, response);

        var result = await _sut.ClassifyDocumentAsync(
            Encoding.UTF8.GetBytes("test"), "application/pdf");

        result.Should().NotBeNull();
        result.DocumentType.Should().Be("Invoice");
        result.Confidence.Should().Be(0.92m);
        result.RawText.Should().Be("FATURA TEST");
    }

    [Fact]
    public async Task ClassifyDocument_MesaDown_FallsBackToMock()
    {
        SetupHttpException();

        var result = await _sut.ClassifyDocumentAsync(
            Encoding.UTF8.GetBytes("FATURA"), "application/pdf");

        // Mock fallback classifies PDF with "FATURA" as Invoice
        result.Should().NotBeNull();
        result.DocumentType.Should().Be("Invoice");
    }

    [Fact]
    public async Task ClassifyDocument_HttpError_FallsBackToMock()
    {
        SetupHttpResponse(HttpStatusCode.InternalServerError, "{}");

        var result = await _sut.ClassifyDocumentAsync(
            Encoding.UTF8.GetBytes("FATURA"), "application/pdf");

        result.Should().NotBeNull();
        result.DocumentType.Should().Be("Invoice");
    }

    [Fact]
    public async Task ClassifyDocument_NullResponse_FallsBackToMock()
    {
        SetupHttpResponse(HttpStatusCode.OK, "null");

        var result = await _sut.ClassifyDocumentAsync(
            Encoding.UTF8.GetBytes("FATURA"), "application/pdf");

        result.Should().NotBeNull();
    }

    // ── ExtractDataAsync ────────────────────────────────────────────

    [Fact]
    public async Task ExtractData_Success_ReturnsExtraction()
    {
        var response = JsonSerializer.Serialize(new
        {
            amount = 5000m,
            taxAmount = 900m,
            counterpartyName = "Test Firma",
            vkn = "1234567890",
            date = DateTime.UtcNow.Date
        });

        SetupHttpResponse(HttpStatusCode.OK, response);

        var classification = new DocumentClassification("Invoice", 0.90m, "Test");
        var result = await _sut.ExtractDataAsync(
            Encoding.UTF8.GetBytes("test"), classification);

        result.Should().NotBeNull();
        result.Amount.Should().Be(5000m);
        result.TaxAmount.Should().Be(900m);
        result.CounterpartyName.Should().Be("Test Firma");
    }

    [Fact]
    public async Task ExtractData_MesaDown_FallsBackToMock()
    {
        SetupHttpException();

        var classification = new DocumentClassification("Invoice", 0.85m, "FATURA");
        var result = await _sut.ExtractDataAsync(
            Encoding.UTF8.GetBytes("test"), classification);

        result.Should().NotBeNull();
        // Mock returns default Invoice extraction
        result.Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task ExtractData_HttpError_FallsBackToMock()
    {
        SetupHttpResponse(HttpStatusCode.BadRequest, "{}");

        var classification = new DocumentClassification("Invoice", 0.85m, "test");
        var result = await _sut.ExtractDataAsync(
            Encoding.UTF8.GetBytes("test"), classification);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtractData_NullResponse_FallsBackToMock()
    {
        SetupHttpResponse(HttpStatusCode.OK, "null");

        var classification = new DocumentClassification("Invoice", 0.85m, "test");
        var result = await _sut.ExtractDataAsync(
            Encoding.UTF8.GetBytes("test"), classification);

        result.Should().NotBeNull();
    }

    // ── SuggestReconciliationAsync ──────────────────────────────────

    [Fact]
    public async Task SuggestReconciliation_Success_ReturnsSuggestion()
    {
        var batchId = Guid.NewGuid();
        var txId = Guid.NewGuid();

        var response = JsonSerializer.Serialize(new
        {
            settlementBatchId = batchId,
            bankTransactionId = txId,
            confidence = 0.88m,
            reason = "AI matched"
        });

        SetupHttpResponse(HttpStatusCode.OK, response);

        var result = await _sut.SuggestReconciliationAsync(
            batchId, new List<Guid> { txId });

        result.Should().NotBeNull();
        result.SettlementBatchId.Should().Be(batchId);
        result.BankTransactionId.Should().Be(txId);
        result.Confidence.Should().Be(0.88m);
        result.Reason.Should().Be("AI matched");
    }

    [Fact]
    public async Task SuggestReconciliation_MesaDown_FallsBackToMock()
    {
        SetupHttpException();

        var batchId = Guid.NewGuid();
        var txId = Guid.NewGuid();

        var result = await _sut.SuggestReconciliationAsync(
            batchId, new List<Guid> { txId });

        result.Should().NotBeNull();
        result.SettlementBatchId.Should().Be(batchId);
        result.Confidence.Should().Be(0.75m); // Mock returns 0.75
    }

    [Fact]
    public async Task SuggestReconciliation_HttpError_FallsBackToMock()
    {
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, "{}");

        var result = await _sut.SuggestReconciliationAsync(
            Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SuggestReconciliation_NullResponse_FallsBackToMock()
    {
        SetupHttpResponse(HttpStatusCode.OK, "null");

        var result = await _sut.SuggestReconciliationAsync(
            Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SuggestReconciliation_EmptyCandidates_MockReturnEmptyGuid()
    {
        SetupHttpException();

        var batchId = Guid.NewGuid();
        var result = await _sut.SuggestReconciliationAsync(batchId, new List<Guid>());

        result.Should().NotBeNull();
        result.BankTransactionId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task SuggestReconciliation_NullReason_DefaultsToMesaAiMatch()
    {
        var batchId = Guid.NewGuid();
        var txId = Guid.NewGuid();

        var response = JsonSerializer.Serialize(new
        {
            settlementBatchId = batchId,
            bankTransactionId = txId,
            confidence = 0.90m,
            reason = (string?)null
        });

        SetupHttpResponse(HttpStatusCode.OK, response);

        var result = await _sut.SuggestReconciliationAsync(
            batchId, new List<Guid> { txId });

        result.Reason.Should().Be("MESA AI match");
    }
}
