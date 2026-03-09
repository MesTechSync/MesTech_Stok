using System.Net;
using System.Net.Http;
using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Invoice;

/// <summary>
/// Parasut e-Fatura provider contract tests — OAuth2 + JSON:API.
/// 12 WireMock tests covering all IInvoiceProvider methods + OAuth flow + error scenarios.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Parasut")]
public class ParasutInvoiceProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly ILogger<ParasutInvoiceProvider> _providerLogger;
    private readonly ILogger<OAuth2AuthProvider> _authLogger;

    private const string TestCompanyId = "12345";
    private const string TestClientId = "test-parasut-client";
    private const string TestClientSecret = "test-parasut-secret";
    private const string TestAccessToken = "parasut-access-token-abc123";
    private const string TestRefreshToken = "parasut-refresh-token-xyz789";
    private const string TestGibInvoiceId = "e-inv-001";

    public ParasutInvoiceProviderTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _providerLogger = new Mock<ILogger<ParasutInvoiceProvider>>().Object;
        _authLogger = new Mock<ILogger<OAuth2AuthProvider>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ────────────────────────────────────────────────────────

    private void StubTokenEndpoint()
    {
        _fixture.Server
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""access_token"": ""{TestAccessToken}"",
                    ""token_type"": ""Bearer"",
                    ""expires_in"": 7200,
                    ""refresh_token"": ""{TestRefreshToken}""
                }}"));
    }

    private ParasutInvoiceProvider CreateProvider()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new ParasutInvoiceProvider(httpClient, _providerLogger);
    }

    private ParasutInvoiceProvider CreateConfiguredProvider()
    {
        StubTokenEndpoint();

        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var tokenCache = new InMemoryTokenCacheProvider();
        var authProvider = new OAuth2AuthProvider(
            platformCode: "Parasut",
            httpClient: httpClient,
            tokenCache: tokenCache,
            clientId: TestClientId,
            clientSecret: TestClientSecret,
            tokenEndpoint: $"{_fixture.BaseUrl}/oauth/token",
            scope: null,
            logger: _authLogger);

        var provider = new ParasutInvoiceProvider(httpClient, _providerLogger);
        provider.Configure(TestCompanyId, authProvider, _fixture.BaseUrl);
        return provider;
    }

    private static InvoiceDto CreateTestInvoice(string number = "PST-2026-001")
    {
        return new InvoiceDto(
            InvoiceNumber: number,
            CustomerName: "Test Musteri Ltd.",
            CustomerTaxNumber: "9876543210",
            CustomerTaxOffice: "Besiktas",
            CustomerAddress: "Istanbul, Turkiye",
            SubTotal: 500m,
            TaxTotal: 100m,
            GrandTotal: 600m,
            Lines: new List<InvoiceLineDto>
            {
                new("Urun X", "SKU-X01", 5, 100m, 20m, 100m, 600m)
            }
        );
    }

    // ════ 1. CreateEFatura — e-Invoice via JSON:API ════

    [Fact]
    public async Task CreateEFatura_ValidInvoice_ReturnsJsonApiResult()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildResource("e_invoices", "inv-001", new Dictionary<string, object>
                {
                    ["gib_invoice_id"] = "GIB-PST-001",
                    ["pdf_url"] = "https://parasut.example.com/pdf/inv-001",
                    ["status"] = "sent"
                })));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB-PST-001");
        result.PdfUrl.Should().Contain("inv-001");
    }

    // ════ 2. CreateEArsiv — e-Archive via JSON:API ════

    [Fact]
    public async Task CreateEArsiv_ValidInvoice_ReturnsJsonApiResult()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_archives")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildResource("e_archives", "arc-001", new Dictionary<string, object>
                {
                    ["gib_invoice_id"] = "GIB-ARC-001",
                    ["pdf_url"] = "https://parasut.example.com/pdf/arc-001",
                    ["status"] = "sent"
                })));

        // Act
        var result = await provider.CreateEArsivAsync(CreateTestInvoice("ARC-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB-ARC-001");
    }

    // ════ 3. CreateEIrsaliye — e-Dispatch via JSON:API ════

    [Fact]
    public async Task CreateEIrsaliye_ValidDispatch_ReturnsJsonApiResult()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildResource("e_dispatch", "dsp-001", new Dictionary<string, object>
                {
                    ["gib_invoice_id"] = "GIB-DSP-001",
                    ["pdf_url"] = (object)null!,
                    ["status"] = "sent"
                })));

        // Act
        var result = await provider.CreateEIrsaliyeAsync(CreateTestInvoice("DSP-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB-DSP-001");
    }

    // ════ 4. CheckStatus — JSON:API status query ════

    [Fact]
    public async Task CheckStatus_ExistingInvoice_ReturnsStatus()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices/{TestGibInvoiceId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildResource("e_invoices", TestGibInvoiceId, new Dictionary<string, object>
                {
                    ["status"] = "accepted",
                    ["accepted_at"] = "2026-03-09T15:00:00Z",
                    ["error_message"] = (object)null!
                })));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("accepted");
        result.AcceptedAt.Should().NotBeNull();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
    }

    // ════ 5. GetPdf — PDF download ════

    [Fact]
    public async Task GetPdf_ExistingInvoice_ReturnsPdfBytes()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var fakePdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices/{TestGibInvoiceId}/pdf")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(fakePdf));

        // Act
        var result = await provider.GetPdfAsync(TestGibInvoiceId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
    }

    // ════ 6. IsEInvoiceTaxpayer — Registered VKN ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_RegisteredVKN_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "9876543210";

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoice_inboxes")
                .WithParam("filter[vkn]", vkn)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildCollection("e_invoice_inboxes",
                    new List<(string, Dictionary<string, object>)>
                    {
                        ("inbox-001", new Dictionary<string, object>
                        {
                            ["vkn"] = vkn,
                            ["title"] = "Test Musteri Ltd."
                        })
                    }, 1, 1, 25)));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 7. IsEInvoiceTaxpayer — Unregistered VKN ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_UnregisteredVKN_ReturnsFalse()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "0000000000";

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoice_inboxes")
                .WithParam("filter[vkn]", vkn)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildEmptyCollection()));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeFalse();
    }

    // ════ 8. CancelInvoice — DELETE ════

    [Fact]
    public async Task CancelInvoice_ValidId_ReturnsSuccess()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices/{TestGibInvoiceId}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(204));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
    }

    // ════ 9. EnsureConfigured guard ════

    [Fact]
    public async Task EnsureConfigured_ThrowsWhenNotConfigured()
    {
        // Arrange — do NOT call Configure()
        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.CreateEFaturaAsync(CreateTestInvoice());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    // ════ 10. Validation error — 422 ════

    [Fact]
    public async Task CreateEFatura_ValidationError_ReturnsFalse()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildError(422,
                    "Validation Error",
                    "customer_tax_number is required")));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 11. CheckStatus — Server error ════

    [Fact]
    public async Task CheckStatus_ServerError_ReturnsError()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices/{TestGibInvoiceId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Error");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 12. CancelInvoice — Server error ════

    [Fact]
    public async Task CancelInvoice_ServerError_ReturnsFalse()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices/{TestGibInvoiceId}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 13. CreateBulkInvoice — Success ════

    [Fact]
    public async Task CreateBulkInvoice_Success_ReturnsAllResults()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var invoices = new[]
        {
            CreateTestInvoice("BULK-001"),
            CreateTestInvoice("BULK-002")
        };

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices/bulk")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildCollection("e_invoice",
                    new List<(string, Dictionary<string, object>)>
                    {
                        ("bulk-inv-001", new Dictionary<string, object>
                        {
                            ["gib_invoice_id"] = "GIB-BULK-001",
                            ["pdf_url"] = "https://parasut.example.com/pdf/bulk-001"
                        }),
                        ("bulk-inv-002", new Dictionary<string, object>
                        {
                            ["gib_invoice_id"] = "GIB-BULK-002",
                            ["pdf_url"] = "https://parasut.example.com/pdf/bulk-002"
                        })
                    }, 2, 1, 25)));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(invoices);

        // Assert
        result.Results.Should().HaveCount(2);
        result.SuccessCount.Should().Be(2);
        result.FailCount.Should().Be(0);
        result.Results[0].Success.Should().BeTrue();
        result.Results[0].GibInvoiceId.Should().Be("GIB-BULK-001");
        result.Results[0].PdfUrl.Should().Contain("bulk-001");
        result.Results[1].Success.Should().BeTrue();
        result.Results[1].GibInvoiceId.Should().Be("GIB-BULK-002");
    }

    // ════ 14. CreateBulkInvoice — Partial response ════

    [Fact]
    public async Task CreateBulkInvoice_PartialResponse_CountsCorrectly()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var invoices = new[]
        {
            CreateTestInvoice("PART-001"),
            CreateTestInvoice("PART-002")
        };

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices/bulk")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonApiHelper.BuildCollection("e_invoice",
                    new List<(string, Dictionary<string, object>)>
                    {
                        ("part-inv-001", new Dictionary<string, object>
                        {
                            ["gib_invoice_id"] = "GIB-PART-001",
                            ["pdf_url"] = "https://parasut.example.com/pdf/part-001"
                        }),
                        ("part-inv-002", new Dictionary<string, object>
                        {
                            ["gib_invoice_id"] = (object)null!,
                            ["pdf_url"] = (object)null!
                        })
                    }, 2, 1, 25)));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(invoices);

        // Assert
        result.Results.Should().HaveCount(2);
        result.SuccessCount.Should().Be(1);
        result.FailCount.Should().Be(1);
        result.Results[0].Success.Should().BeTrue();
        result.Results[0].GibInvoiceId.Should().Be("GIB-PART-001");
        result.Results[1].Success.Should().BeFalse();
        result.Results[1].ErrorMessage.Should().Contain("Missing gib_invoice_id");
    }

    // ════ 15. CreateBulkInvoice — HTTP error ════

    [Fact]
    public async Task CreateBulkInvoice_HttpError_ReturnsAllFail()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var invoices = new[]
        {
            CreateTestInvoice("ERR-001"),
            CreateTestInvoice("ERR-002")
        };

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/v4/{TestCompanyId}/e_invoices/bulk")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(invoices);

        // Assert
        result.Results.Should().HaveCount(2);
        result.SuccessCount.Should().Be(0);
        result.FailCount.Should().Be(2);
        result.Results.Should().AllSatisfy(r =>
        {
            r.Success.Should().BeFalse();
            r.ErrorMessage.Should().NotBeNullOrEmpty();
        });
    }
}
