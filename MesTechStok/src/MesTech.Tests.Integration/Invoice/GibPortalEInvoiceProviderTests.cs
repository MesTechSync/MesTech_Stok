using System.Net;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Invoice;

/// <summary>
/// GIB e-Arsiv Portal IEInvoiceProvider WireMock integration tests.
/// REST-based token auth (FormUrlEncoded login), JSON dispatch endpoints.
/// 12 tests covering SendAsync, GetPdfUrlAsync, CancelAsync, CheckVknMukellefAsync,
/// GetCreditBalanceAsync, and PingAsync.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "GibPortalEInvoice")]
public class GibPortalEInvoiceProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly ILogger<GibPortalEInvoiceProvider> _logger;

    private const string LoginPath = "/earsiv-services/assos-login";
    private const string DispatchPath = "/earsiv-services/dispatch";
    private const string DownloadPath = "/earsiv-services/download";
    private const string TestUserId = "1234567890";
    private const string TestPassword = "test-portal-pass";
    private const string TestToken = "mock-gib-token-abc123";

    public GibPortalEInvoiceProviderTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _logger = new Mock<ILogger<GibPortalEInvoiceProvider>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ──────────────────────────────────────────────────────────

    private GibPortalEInvoiceProvider CreateProvider(string? mukellefUrl = null)
    {
        var options = new GibPortalEInvoiceOptions
        {
            BaseUrl = _fixture.BaseUrl,
            UserId = TestUserId,
            Password = TestPassword,
            MukellefQueryUrl = mukellefUrl ?? $"{_fixture.BaseUrl}/earsiv-services/dispatch",
            Enabled = true
        };

        var httpClient = new HttpClient();
        return new GibPortalEInvoiceProvider(
            httpClient,
            _logger,
            Options.Create(options));
    }

    private static EInvoiceDocument CreateTestDocument()
    {
        var doc = EInvoiceDocument.Create(
            gibUuid: Guid.NewGuid().ToString(),
            ettnNo: "GIB2026032800001",
            scenario: EInvoiceScenario.EARSIVFATURA,
            type: EInvoiceType.SATIS,
            issueDate: new DateTime(2026, 3, 28, 10, 30, 0),
            sellerVkn: "1234567890",
            sellerTitle: "MesTech Yazilim A.S.",
            buyerTitle: "Test Musteri Ltd.",
            providerId: "GibPortal",
            createdBy: "test-user");

        doc.SetFinancials(
            lineExtension: 1000m,
            taxExclusive: 1000m,
            taxInclusive: 1200m,
            allowance: 0m,
            taxAmount: 200m,
            payable: 1200m,
            currency: "TRY");

        return doc;
    }

    /// <summary>
    /// Stubs the login endpoint to return a valid token JSON.
    /// Must be called before any operation that triggers EnsureTokenAsync.
    /// </summary>
    private void StubLoginSuccess()
    {
        _fixture.Server
            .Given(Request.Create().WithPath(LoginPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""token"":""{TestToken}""}}"));
    }

    private void StubLoginFailure()
    {
        _fixture.Server
            .Given(Request.Create().WithPath(LoginPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("Unauthorized"));
    }

    // ════ 1. SendAsync — success ════

    [Fact]
    public async Task SendAsync_Success_ReturnsProviderRef()
    {
        // Arrange
        StubLoginSuccess();

        var uuid = Guid.NewGuid().ToString();
        _fixture.Server
            .Given(Request.Create().WithPath(DispatchPath).UsingPost()
                .WithHeader("Token", TestToken))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""uuid"":""{uuid}"",""success"":true}}"));

        var provider = CreateProvider();
        var document = CreateTestDocument();

        // Act
        var result = await provider.SendAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.ProviderRef.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
        result.CreditUsed.Should().Be(0);
    }

    // ════ 2. SendAsync — server error ════

    [Fact]
    public async Task SendAsync_ServerError_ReturnsFailure()
    {
        // Arrange
        StubLoginSuccess();

        _fixture.Server
            .Given(Request.Create().WithPath(DispatchPath).UsingPost()
                .WithHeader("Token", TestToken))
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var provider = CreateProvider();
        var document = CreateTestDocument();

        // Act
        var result = await provider.SendAsync(document);

        // Assert
        result.Success.Should().BeFalse();
        result.ProviderRef.Should().BeNull();
        result.ErrorMessage.Should().Contain("500");
        result.CreditUsed.Should().Be(0);
    }

    // ════ 3. GetPdfUrlAsync — success ════

    [Fact]
    public async Task GetPdfUrlAsync_Success_ReturnsDownloadUrl()
    {
        // Arrange
        StubLoginSuccess();

        var providerRef = Guid.NewGuid().ToString();

        _fixture.Server
            .Given(Request.Create().WithPath(DownloadPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(new byte[] { 0x25, 0x50, 0x44, 0x46 })); // %PDF

        var provider = CreateProvider();

        // Act
        var url = await provider.GetPdfUrlAsync(providerRef);

        // Assert
        url.Should().NotBeNullOrEmpty();
        url.Should().Contain("/earsiv-services/download");
        url.Should().Contain("ettn=");
        url.Should().Contain("onizleme=Y");
    }

    // ════ 4. GetPdfUrlAsync — not found returns null ════

    [Fact]
    public async Task GetPdfUrlAsync_NotFound_ReturnsNull()
    {
        // Arrange
        StubLoginSuccess();

        _fixture.Server
            .Given(Request.Create().WithPath(DownloadPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody("Not Found"));

        var provider = CreateProvider();

        // Act
        var url = await provider.GetPdfUrlAsync("nonexistent-ettn");

        // Assert
        url.Should().BeNull();
    }

    // ════ 5. CancelAsync — success ════

    [Fact]
    public async Task CancelAsync_Success_ReturnsTrue()
    {
        // Arrange
        StubLoginSuccess();

        _fixture.Server
            .Given(Request.Create().WithPath(DispatchPath).UsingPost()
                .WithHeader("Token", TestToken))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var provider = CreateProvider();

        // Act
        var result = await provider.CancelAsync("test-ettn-uuid", "Yanlis fatura kesildi");

        // Assert
        result.Should().BeTrue();
    }

    // ════ 6. CancelAsync — already cancelled (server error) ════

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ReturnsFalse()
    {
        // Arrange
        StubLoginSuccess();

        _fixture.Server
            .Given(Request.Create().WithPath(DispatchPath).UsingPost()
                .WithHeader("Token", TestToken))
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody(@"{""error"":""Bu fatura zaten iptal edilmistir""}"));

        var provider = CreateProvider();

        // Act
        var result = await provider.CancelAsync("already-cancelled-ettn", "Tekrar iptal");

        // Assert
        result.Should().BeFalse();
    }

    // ════ 7. CheckVknMukellefAsync — registered (eFatura) ════

    [Fact]
    public async Task CheckVknMukellefAsync_Registered_ReturnsEInvoiceMukellef()
    {
        // Arrange — mukellef endpoint is separate (GIB public, no auth)
        _fixture.Server
            .Given(Request.Create().WithPath(DispatchPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""unvan"":""Test Ticaret A.S."",
                    ""eFaturaMukellef"":true,
                    ""eArsivMukellef"":true
                }"));

        var provider = CreateProvider();

        // Act
        var result = await provider.CheckVknMukellefAsync("1234567890");

        // Assert
        result.Vkn.Should().Be("1234567890");
        result.IsEInvoiceMukellef.Should().BeTrue();
        result.IsEArchiveMukellef.Should().BeTrue();
        result.Title.Should().Be("Test Ticaret A.S.");
        result.CheckedAt.Should().NotBeNull();
    }

    // ════ 8. CheckVknMukellefAsync — unregistered ════

    [Fact]
    public async Task CheckVknMukellefAsync_Unregistered_ReturnsFalse()
    {
        // Arrange
        _fixture.Server
            .Given(Request.Create().WithPath(DispatchPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""eFaturaMukellef"":false,
                    ""eArsivMukellef"":false
                }"));

        var provider = CreateProvider();

        // Act
        var result = await provider.CheckVknMukellefAsync("9999999999");

        // Assert
        result.Vkn.Should().Be("9999999999");
        result.IsEInvoiceMukellef.Should().BeFalse();
        result.IsEArchiveMukellef.Should().BeFalse();
        result.Title.Should().BeNull();
    }

    // ════ 9. CheckVknMukellefAsync — server error returns safe default ════

    [Fact]
    public async Task CheckVknMukellefAsync_ServerError_ReturnsSafeDefault()
    {
        // Arrange
        _fixture.Server
            .Given(Request.Create().WithPath(DispatchPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var provider = CreateProvider();

        // Act
        var result = await provider.CheckVknMukellefAsync("5555555555");

        // Assert
        result.Vkn.Should().Be("5555555555");
        result.IsEInvoiceMukellef.Should().BeFalse();
        result.IsEArchiveMukellef.Should().BeFalse();
        result.Title.Should().BeNull();
        result.CheckedAt.Should().NotBeNull();
    }

    // ════ 10. GetCreditBalanceAsync — returns -1 (unlimited) ════

    [Fact]
    public async Task GetCreditBalanceAsync_ReturnsNegativeOne_Unlimited()
    {
        // Arrange — no HTTP call needed, method is synchronous
        var provider = CreateProvider();

        // Act
        var balance = await provider.GetCreditBalanceAsync();

        // Assert
        balance.Should().Be(-1);
    }

    // ════ 11. PingAsync — healthy (default implementation returns true) ════

    [Fact]
    public async Task PingAsync_Healthy_ReturnsTrue()
    {
        // Arrange — default IEInvoiceProvider.PingAsync returns true
        IEInvoiceProvider provider = CreateProvider();

        // Act
        var healthy = await provider.PingAsync();

        // Assert
        healthy.Should().BeTrue();
    }

    // ════ 12. SendAsync — login failure propagates error ════

    [Fact]
    public async Task SendAsync_LoginFailure_ReturnsFailureWithMessage()
    {
        // Arrange — login returns 401
        StubLoginFailure();

        var provider = CreateProvider();
        var document = CreateTestDocument();

        // Act
        var result = await provider.SendAsync(document);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("login failed");
    }

    // ════ 13. ProviderCode — returns GibPortal ════

    [Fact]
    public void ProviderCode_ReturnsGibPortal()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        provider.ProviderCode.Should().Be("GibPortal");
    }

    // ════ 14. CheckVknMukellefAsync — title only (eArsiv inferred) ════

    [Fact]
    public async Task CheckVknMukellefAsync_TitleOnly_InfersEArsiv()
    {
        // Arrange — GIB returns unvan but no explicit mukellef flags
        _fixture.Server
            .Given(Request.Create().WithPath(DispatchPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""unvan"":""Kucuk Isletme Ltd. Sti.""}"));

        var provider = CreateProvider();

        // Act
        var result = await provider.CheckVknMukellefAsync("1111111111");

        // Assert — title present but no eFatura flag → eArsiv inferred true
        result.IsEInvoiceMukellef.Should().BeFalse();
        result.IsEArchiveMukellef.Should().BeTrue();
        result.Title.Should().Be("Kucuk Isletme Ltd. Sti.");
    }
}
