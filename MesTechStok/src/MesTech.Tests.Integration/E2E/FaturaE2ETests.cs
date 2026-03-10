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

namespace MesTech.Tests.Integration.E2E;

/// <summary>
/// D-19: E2E fatura akisi — siparis -> fatura -> PDF -> iptal -> iade.
/// TrendyolEFaturamAdapter + WireMockFixture. Testcontainers gerekmez:
/// adapter sadece HTTP yapar, DB'ye dokunmaz.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Feature", "FaturaAkisi")]
[Trait("Phase", "Dalga5")]
public class FaturaE2ETests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private const long SupplierId = 123456;
    private const string SupplierPath = "/suppliers/123456/e-invoices";
    private const string GibId = "GIB2026031099001";

    public FaturaE2ETests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    public void Dispose() => _fixture.Reset();

    private IInvoiceAdapter CreateAdapter()
    {
        var http = new System.Net.Http.HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var pLogger = new Mock<ILogger<TrendyolEFaturamProvider>>().Object;
        var provider = new TrendyolEFaturamProvider(http, pLogger);
        provider.Configure("e2e-test-key", SupplierId, _fixture.BaseUrl);
        var aLogger = new Mock<ILogger<TrendyolEFaturamAdapter>>().Object;
        return new TrendyolEFaturamAdapter(provider, aLogger);
    }

    private static InvoiceCreateRequest BuildOrderRequest(string orderId = "TY-E2E-001")
    {
        return new InvoiceCreateRequest
        {
            OrderId = Guid.NewGuid(),
            Platform = PlatformType.Trendyol,
            PlatformOrderId = orderId,
            Type = InvoiceType.EFatura,
            Customer = new InvoiceCustomerInfo(
                "E2E Test Firma A.S.", "1234567890", "Kadikoy", "Istanbul, Turkiye", null, null),
            TotalAmount = 1180m,
            DefaultKdv = KdvRate.Yuzde20,
            Lines = new List<InvoiceCreateLine>
            {
                new("Test Urun", "SKU-E2E-001", 2, 500m, 0.18m, null)
            }
        };
    }

    // ════ 1. Siparis -> Fatura ════
    // InvoiceCreateRequest (siparis verisi) -> adapter -> POST efatura -> GibInvoiceId.

    [Fact]
    public async Task FaturaAkisi_Siparis_FaturaOlusturur()
    {
        var adapter = CreateAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/efatura").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""gibInvoiceId"": ""{GibId}"", ""pdfUrl"": ""https://efaturam.trendyol.com/pdf/{GibId}""}}"));

        var result = await adapter.CreateInvoiceAsync(BuildOrderRequest());

        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be(GibId);
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 2. Fatura -> PDF ════
    // GibInvoiceId ile PDF byte[] indirme.

    [Fact]
    public async Task FaturaAkisi_Fatura_PdfIndirir()
    {
        var adapter = CreateAdapter();
        var fakePdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/{GibId}/pdf").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(fakePdf));

        var pdf = await adapter.GetInvoicePdfAsync(GibId);

        pdf.Should().NotBeEmpty();
        pdf[0].Should().Be(0x25); // %PDF magic bytes
    }

    // ════ 3. Fatura -> Durum Sorgulama ════
    // GIB'den fatura kabul durumu sorgusu.

    [Fact]
    public async Task FaturaAkisi_Fatura_StatusSorgular()
    {
        var adapter = CreateAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/{GibId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"": ""Accepted"", ""acceptedAt"": ""2026-03-10T14:00:00Z"", ""errorMessage"": null}"));

        var status = await adapter.GetInvoiceStatusAsync(GibId);

        status.GibInvoiceId.Should().Be(GibId);
        status.Status.Should().Be(InvoiceStatus.Accepted);
        status.Description.Should().Be("Accepted");
        status.ResponseDate.Should().NotBeNull();
    }

    // ════ 4. Fatura -> Iptal + Durum Dogrulama ════
    // Iptal sonrasi status "Cancelled" olmali.

    [Fact]
    public async Task FaturaAkisi_Fatura_Iptal()
    {
        var adapter = CreateAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/{GibId}/cancel").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/{GibId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"": ""Cancelled"", ""acceptedAt"": null, ""errorMessage"": null}"));

        var cancelResult = await adapter.CancelInvoiceAsync(GibId, "Musteri talebi — siparis iptali");
        var statusAfter = await adapter.GetInvoiceStatusAsync(GibId);

        cancelResult.Success.Should().BeTrue();
        cancelResult.ErrorMessage.Should().BeNull();
        statusAfter.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    // ════ 5. Fatura -> Iade ════
    // Orijinal fatura iptal + iade e-arsiv faturasi olustur.

    [Fact]
    public async Task FaturaAkisi_Iade_IadeFaturasiOlusturur()
    {
        var adapter = CreateAdapter();
        const string iadeGibId = "GIB2026031099002";

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/{GibId}/cancel").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/earsiv").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""gibInvoiceId"": ""{iadeGibId}"", ""pdfUrl"": null}}"));

        var cancelResult = await adapter.CancelInvoiceAsync(GibId, "Iade talebi");
        var iadeRequest = BuildOrderRequest("TY-IADE-001") with { Type = InvoiceType.EArsiv };
        var iadeResult = await adapter.CreateInvoiceAsync(iadeRequest);

        cancelResult.Success.Should().BeTrue();
        iadeResult.Success.Should().BeTrue();
        iadeResult.GibInvoiceId.Should().Be(iadeGibId);
        iadeResult.GibInvoiceId.Should().NotBe(GibId);
    }
}
