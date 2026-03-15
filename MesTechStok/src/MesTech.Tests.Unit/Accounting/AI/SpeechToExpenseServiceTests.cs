using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.AI.Accounting;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Accounting.AI;

/// <summary>
/// SpeechToExpenseService tests — audio → STT → expense extraction pipeline.
/// Tests MESA STT endpoint interaction, fallback behavior, and document creation.
/// </summary>
[Trait("Category", "Unit")]
public class SpeechToExpenseServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IMesaAccountingService> _accountingServiceMock;
    private readonly Mock<IAccountingDocumentRepository> _documentRepoMock;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SpeechToExpenseService _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SpeechToExpenseServiceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        _accountingServiceMock = new Mock<IMesaAccountingService>();
        _documentRepoMock = new Mock<IAccountingDocumentRepository>();
        _tenantProviderMock = new Mock<ITenantProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);

        _sut = new SpeechToExpenseService(
            _httpClient,
            _accountingServiceMock.Object,
            _documentRepoMock.Object,
            _tenantProviderMock.Object,
            _unitOfWorkMock.Object,
            new Mock<ILogger<SpeechToExpenseService>>().Object);
    }

    private void SetupSttResponse(string text, decimal confidence)
    {
        var sttJson = JsonSerializer.Serialize(new { text, confidence });
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sttJson, Encoding.UTF8, "application/json")
            });
    }

    private void SetupSttFailure(HttpStatusCode status)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent("{}")
            });
    }

    private void SetupExtraction(decimal amount, string? counterpartyName = null)
    {
        _accountingServiceMock.Setup(s => s.ExtractDataAsync(
                It.IsAny<byte[]>(),
                It.IsAny<DocumentClassification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentExtraction(
                Amount: amount,
                TaxAmount: null,
                CounterpartyName: counterpartyName,
                VKN: null,
                Date: DateTime.UtcNow,
                ExtraFields: new Dictionary<string, string> { ["category"] = "Genel" }));
    }

    // ── ProcessAudio Tests ──

    [Fact]
    public async Task ProcessAudio_ValidAudio_ReturnsExpenses()
    {
        // Arrange
        var audioData = new byte[] { 0x01, 0x02, 0x03 };
        SetupSttResponse("250 lira ofis malzemesi", 0.92m);
        SetupExtraction(250m, "Ofis Deposu");

        // Act
        var result = await _sut.ProcessAudioAsync(audioData, "audio/wav", _tenantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(1);
        result.First().Amount.Should().Be(250m);
    }

    [Fact]
    public async Task ProcessAudio_MultipleItems_ReturnsMultiple()
    {
        // Arrange
        var audioData = new byte[] { 0x01, 0x02, 0x03 };
        SetupSttResponse("ofis malzemesi 100 lira, kargo 50 lira", 0.90m);

        var itemsJson = JsonSerializer.Serialize(new[]
        {
            new { title = "Ofis Malzemesi", amount = 100m, category = "Ofis" },
            new { title = "Kargo", amount = 50m, category = "Lojistik" }
        });

        _accountingServiceMock.Setup(s => s.ExtractDataAsync(
                It.IsAny<byte[]>(),
                It.IsAny<DocumentClassification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentExtraction(
                Amount: 100m,
                TaxAmount: null,
                CounterpartyName: "Test",
                VKN: null,
                Date: DateTime.UtcNow,
                ExtraFields: new Dictionary<string, string>
                {
                    ["category"] = "Genel",
                    ["items"] = itemsJson
                }));

        // Act
        var result = await _sut.ProcessAudioAsync(audioData, "audio/wav", _tenantId);

        // Assert — should have main item + 2 sub-items = 3
        result.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task ProcessAudio_MesaDown_FallbackReturnsEmpty()
    {
        // Arrange
        var audioData = new byte[] { 0x01, 0x02, 0x03 };
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("MESA unreachable"));

        // Act
        var result = await _sut.ProcessAudioAsync(audioData, "audio/wav", _tenantId);

        // Assert — Demir Kural #12: returns empty, does not throw
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAudio_NullAudio_ThrowsArgumentNull()
    {
        // Act
        var act = () => _sut.ProcessAudioAsync(null!, "audio/wav", _tenantId);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessAudio_NullMimeType_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.ProcessAudioAsync(new byte[] { 1 }, null!, _tenantId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessAudio_EmptyMimeType_ThrowsArgumentException()
    {
        var act = () => _sut.ProcessAudioAsync(new byte[] { 1 }, "  ", _tenantId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessAudio_CreatesAccountingDocuments()
    {
        // Arrange
        var audioData = new byte[] { 0x01, 0x02 };
        SetupSttResponse("500 lira kargo", 0.95m);
        SetupExtraction(500m, "Kargo Firması");

        // Act
        var result = await _sut.ProcessAudioAsync(audioData, "audio/wav", _tenantId);

        // Assert
        result.Should().HaveCountGreaterOrEqualTo(1);
        _documentRepoMock.Verify(r => r.AddAsync(
            It.IsAny<MesTech.Domain.Accounting.Entities.AccountingDocument>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAudio_SttReturnsEmptyText_ReturnsEmpty()
    {
        // Arrange
        var sttJson = JsonSerializer.Serialize(new { text = "", confidence = 0m });
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sttJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _sut.ProcessAudioAsync(new byte[] { 1 }, "audio/wav", _tenantId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAudio_SttServerError_ReturnsEmpty()
    {
        // Arrange
        SetupSttFailure(HttpStatusCode.InternalServerError);

        // Act
        var result = await _sut.ProcessAudioAsync(new byte[] { 1 }, "audio/wav", _tenantId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAudio_TaskCanceled_ReturnsEmpty()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timeout"));

        // Act
        var result = await _sut.ProcessAudioAsync(new byte[] { 1 }, "audio/wav", _tenantId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAudio_ExtractionFails_ReturnsEmpty()
    {
        // Arrange
        var audioData = new byte[] { 0x01 };
        SetupSttResponse("gider kaydi", 0.85m);

        _accountingServiceMock.Setup(s => s.ExtractDataAsync(
                It.IsAny<byte[]>(),
                It.IsAny<DocumentClassification>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Extraction failed"));

        // Act
        var result = await _sut.ProcessAudioAsync(audioData, "audio/wav", _tenantId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAudio_ZeroAmount_ReturnsEmptyResults()
    {
        // Arrange
        var audioData = new byte[] { 0x01 };
        SetupSttResponse("bilinmeyen", 0.50m);

        _accountingServiceMock.Setup(s => s.ExtractDataAsync(
                It.IsAny<byte[]>(),
                It.IsAny<DocumentClassification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentExtraction(
                Amount: 0m,
                TaxAmount: null,
                CounterpartyName: null,
                VKN: null,
                Date: null,
                ExtraFields: new Dictionary<string, string>()));

        // Act
        var result = await _sut.ProcessAudioAsync(audioData, "audio/wav", _tenantId);

        // Assert — zero amount should not create expense
        result.Should().BeEmpty();
    }
}
