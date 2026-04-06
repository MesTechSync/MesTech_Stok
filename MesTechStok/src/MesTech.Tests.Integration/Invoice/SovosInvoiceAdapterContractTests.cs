using System.Net.Http;
using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Invoice;

/// <summary>
/// SovosInvoiceAdapter contract tests with WireMock — H31.
/// Tests the IInvoiceAdapter-level operations:
/// 1. CreateInvoice returns GIB invoice ID on success
/// 2. IsEFaturaMukellef returns true for known VKN
/// 3. CancelInvoice returns success
/// 4. GetInvoiceStatus returns current status
/// 5. CreateInvoice returns failure when API is down (500)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Sovos")]
[Trait("Layer", "Adapter")]
public class SovosInvoiceAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly Mock<IGibMukellefService> _gibServiceMock;
    private readonly ILogger<SovosInvoiceAdapter> _adapterLogger;
    private readonly ILogger<SovosInvoiceProvider> _providerLogger;

    private const string TestApiKey = "test-sovos-adapter-key";
    private const string TestGibInvoiceId = "GIB2026031500001";

    public SovosInvoiceAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _gibServiceMock = new Mock<IGibMukellefService>();
        _adapterLogger = new Mock<ILogger<SovosInvoiceAdapter>>().Object;
        _providerLogger = new Mock<ILogger<SovosInvoiceProvider>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ────────────────────────────────────────────────────────

    private SovosInvoiceAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var ublBuilder = new Mock<IUblTrXmlBuilder>().Object;
        var ublValidator = new Mock<IUblTrXmlValidator>().Object;
        var provider = new SovosInvoiceProvider(httpClient, _providerLogger, ublBuilder, ublValidator);
        provider.Configure(TestApiKey, _fixture.BaseUrl);
        return new SovosInvoiceAdapter(provider, _gibServiceMock.Object, _adapterLogger);
    }

    private static InvoiceCreateRequest CreateTestRequest()
    {
        return new InvoiceCreateRequest
        {
            OrderId = Guid.NewGuid(),
            Platform = PlatformType.Trendyol,
            PlatformOrderId = "TRD-2026-001",
            Type = InvoiceType.EFatura,
            Customer = new InvoiceCustomerInfo(
                "Test Musteri A.S.", "1234567890", "Kadikoy", "Istanbul, Turkiye", null, null),
            TotalAmount = 1200m,
            DefaultKdv = KdvRate.Yuzde20,
            Lines = new List<InvoiceCreateLine>
            {
                new("Urun A", "SKU-001", 2, 400m, 0.20m, null),
                new("Urun B", "SKU-002", 1, 200m, 0.20m, null)
            }
        };
    }

    // ══════════════════════════════════════
    // 1. CreateInvoice returns GIB invoice ID on success
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateInvoiceAsync_ValidRequest_ReturnsGibInvoiceId()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/outgoing").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""gibInvoiceId"": ""{TestGibInvoiceId}"",
                    ""pdfUrl"": ""https://sovos.example.com/pdf/{TestGibInvoiceId}""
                }}"));

        // Act
        var result = await adapter.CreateInvoiceAsync(CreateTestRequest());

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.PdfUrl.Should().Contain(TestGibInvoiceId);
        result.ErrorMessage.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 2. IsEFaturaMukellef returns true for known VKN
    // ══════════════════════════════════════

    [Fact]
    public async Task IsEFaturaMukellefAsync_KnownVKN_ReturnsTrue()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();
        var vkn = "1234567890";

        // The adapter delegates to IGibMukellefService
        _gibServiceMock
            .Setup(g => g.IsEFaturaMukellefAsync(vkn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await adapter.IsEFaturaMukellefAsync(vkn);

        // Assert
        result.Should().BeTrue();
        _gibServiceMock.Verify(g => g.IsEFaturaMukellefAsync(vkn, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ══════════════════════════════════════
    // 3. CancelInvoice returns success
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelInvoiceAsync_ValidId_ReturnsSuccess()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/{TestGibInvoiceId}/cancel")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await adapter.CancelInvoiceAsync(TestGibInvoiceId, "Yanlis tutar");

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
    }

    // ══════════════════════════════════════
    // 4. GetInvoiceStatus returns current status
    // ══════════════════════════════════════

    [Fact]
    public async Task GetInvoiceStatusAsync_ExistingInvoice_ReturnsStatus()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/{TestGibInvoiceId}/status")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""Accepted"",
                    ""acceptedAt"": ""2026-03-15T14:30:00Z"",
                    ""errorMessage"": null
                }"));

        // Act
        var result = await adapter.GetInvoiceStatusAsync(TestGibInvoiceId);

        // Assert
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.Status.Should().Be(InvoiceStatus.Sent);
        result.Description.Should().Be("Accepted");
        result.ResponseDate.Should().NotBeNull();
    }

    // ══════════════════════════════════════
    // 5. CreateInvoice returns failure when API is down
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateInvoiceAsync_ServerDown_ReturnsFailure()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/outgoing").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await adapter.CreateInvoiceAsync(CreateTestRequest());

        // Assert
        result.Success.Should().BeFalse();
        result.GibInvoiceId.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
