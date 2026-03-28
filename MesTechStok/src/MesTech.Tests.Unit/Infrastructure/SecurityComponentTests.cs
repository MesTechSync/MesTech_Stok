using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using MesTech.Infrastructure.Integration.Webhooks;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Infrastructure;

// ════════════════════════════════════════════════════════
// 5.D2-03: Security Component Tests
// ════════════════════════════════════════════════════════

// ── AesGcmEncryptionService Tests ──

[Trait("Category", "Unit")]
public class AesGcmEncryptionServiceTests
{
    private readonly string _validKey = AesGcmEncryptionService.GenerateKey();

    [Fact]
    public void EncryptDecrypt_ShouldRoundTrip()
    {
        var service = new AesGcmEncryptionService(_validKey);
        var original = "SuperSecretApiKey_12345!@#";

        var encrypted = service.Encrypt(original);
        var decrypted = service.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentOutputEachTime()
    {
        var service = new AesGcmEncryptionService(_validKey);
        var plainText = "SameInput";

        var encrypted1 = service.Encrypt(plainText);
        var encrypted2 = service.Encrypt(plainText);

        encrypted1.Should().NotBe(encrypted2, "random nonce makes each encryption unique");
    }

    [Fact]
    public void Decrypt_WithWrongKey_ShouldThrow()
    {
        var service1 = new AesGcmEncryptionService(_validKey);
        var differentKey = AesGcmEncryptionService.GenerateKey();
        var service2 = new AesGcmEncryptionService(differentKey);

        var encrypted = service1.Encrypt("secret data");

        var act = () => service2.Decrypt(encrypted);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Constructor_InvalidKeyLength_ShouldThrow()
    {
        var shortKey = Convert.ToBase64String(new byte[16]); // 128-bit, not 256

        var act = () => new AesGcmEncryptionService(shortKey);
        act.Should().Throw<ArgumentException>().WithMessage("*256 bits*");
    }

    [Fact]
    public void GenerateKey_ShouldReturn32ByteBase64()
    {
        var key = AesGcmEncryptionService.GenerateKey();
        var bytes = Convert.FromBase64String(key);
        bytes.Length.Should().Be(32);
    }
}

// ── WebhookReceiverService Tests ──

[Trait("Category", "Unit")]
public class WebhookReceiverServiceTests
{
    private readonly Mock<ILogger<WebhookReceiverService>> _logger = new();

    private static Mock<IIntegratorAdapter> CreateWebhookAdapter(string platformCode)
    {
        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.PlatformCode).Returns(platformCode);
        adapter.As<IWebhookCapableAdapter>()
            .Setup(a => a.ProcessWebhookPayloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return adapter;
    }

    [Fact]
    public async Task ProcessOrderWebhook_KnownPlatform_ShouldSucceed()
    {
        var adapter = CreateWebhookAdapter("trendyol");

        var service = new WebhookReceiverService(
            new IIntegratorAdapter[] { adapter.Object }, _logger.Object);

        var payload = JsonSerializer.Serialize(new { orderNumber = "ORD-123" });
        var result = await service.ProcessOrderWebhookAsync("trendyol", payload);

        result.Success.Should().BeTrue();
        result.EventType.Should().Be("OrderCreated");
        result.PlatformOrderId.Should().Be("ORD-123");
    }

    [Fact]
    public async Task ProcessOrderWebhook_UnknownPlatform_ShouldReturnError()
    {
        var service = new WebhookReceiverService(
            Array.Empty<IIntegratorAdapter>(), _logger.Object);

        var result = await service.ProcessOrderWebhookAsync("unknown", "{}");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("unknown");
    }

    [Fact]
    public async Task ProcessClaimWebhook_ShouldExtractClaimId()
    {
        var adapter = CreateWebhookAdapter("trendyol");

        var service = new WebhookReceiverService(
            new IIntegratorAdapter[] { adapter.Object }, _logger.Object);

        var payload = JsonSerializer.Serialize(new { claimId = "CLM-456" });
        var result = await service.ProcessClaimWebhookAsync("trendyol", payload);

        result.Success.Should().BeTrue();
        result.EventType.Should().Be("ClaimCreated");
        result.PlatformOrderId.Should().Be("CLM-456");
    }

    [Fact]
    public async Task ProcessGenericWebhook_OrderEvent_ShouldRouteToOrderHandler()
    {
        var adapter = CreateWebhookAdapter("opencart");

        var service = new WebhookReceiverService(
            new IIntegratorAdapter[] { adapter.Object }, _logger.Object);

        var payload = JsonSerializer.Serialize(new { orderNumber = "OC-789" });
        var result = await service.ProcessGenericWebhookAsync("opencart", "OrderCreated", payload);

        result.Success.Should().BeTrue();
        result.EventType.Should().Be("OrderCreated");
    }

    [Fact]
    public async Task ProcessGenericWebhook_UnknownEvent_ShouldStillSucceed()
    {
        var adapter = CreateWebhookAdapter("trendyol");

        var service = new WebhookReceiverService(
            new IIntegratorAdapter[] { adapter.Object }, _logger.Object);

        var result = await service.ProcessGenericWebhookAsync("trendyol", "CustomEvent", "{}");

        result.Success.Should().BeTrue();
        result.EventType.Should().Be("CustomEvent");
    }
}

// ── OAuth2AuthProvider Tests ──

[Trait("Category", "Unit")]
public class OAuth2AuthProviderTests
{
    private readonly Mock<ITokenCacheProvider> _tokenCache = new();
    private readonly Mock<ILogger<OAuth2AuthProvider>> _logger = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();

    [Fact]
    public async Task GetToken_CachedAndValid_ShouldReturnCachedToken()
    {
        var cachedToken = new AuthToken("cached-access", null, DateTime.UtcNow.AddHours(1));
        _tokenCache.Setup(c => c.GetAsync("oauth2:amazon", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedToken);

        var httpClient = new HttpClient();
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var provider = new OAuth2AuthProvider(
            "amazon", _httpClientFactory.Object.CreateClient(), _tokenCache.Object,
            "client_id", "client_secret", "https://token.example.com", null, _logger.Object);

        var result = await provider.GetTokenAsync();

        result.AccessToken.Should().Be("cached-access");
        _tokenCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<AuthToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void IsTokenExpired_FreshToken_ShouldReturnFalse()
    {
        var httpClient = new HttpClient();
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var provider = new OAuth2AuthProvider(
            "test", _httpClientFactory.Object.CreateClient(), _tokenCache.Object,
            "id", "secret", "https://token.example.com", null, _logger.Object);

        var freshToken = new AuthToken("access", null, DateTime.UtcNow.AddHours(1));
        provider.IsTokenExpired(freshToken).Should().BeFalse();
    }

    [Fact]
    public void IsTokenExpired_NearExpiry_ShouldReturnTrue()
    {
        var httpClient = new HttpClient();
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var provider = new OAuth2AuthProvider(
            "test", _httpClientFactory.Object.CreateClient(), _tokenCache.Object,
            "id", "secret", "https://token.example.com", null, _logger.Object);

        var nearExpiryToken = new AuthToken("access", null, DateTime.UtcNow.AddMinutes(3));
        provider.IsTokenExpired(nearExpiryToken).Should().BeTrue("5-minute buffer means 3 min left = expired");
    }

    [Fact]
    public async Task GetToken_ExpiredCache_ShouldRequestNewToken()
    {
        var expiredToken = new AuthToken("old", null, DateTime.UtcNow.AddMinutes(-10));
        _tokenCache.Setup(c => c.GetAsync("oauth2:ebay", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var responseJson = JsonSerializer.Serialize(new
        {
            access_token = "new-token",
            expires_in = 3600,
            token_type = "Bearer"
        });

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);

        var provider = new OAuth2AuthProvider(
            "ebay", httpClient, _tokenCache.Object,
            "client_id", "client_secret", "https://api.ebay.com/identity/v1/oauth2/token",
            null, _logger.Object);

        var result = await provider.GetTokenAsync();

        result.AccessToken.Should().Be("new-token");
        _tokenCache.Verify(c => c.SetAsync("oauth2:ebay", It.IsAny<AuthToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetToken_FailedRequest_ShouldThrow()
    {
        _tokenCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthToken?)null);

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("invalid_client", Encoding.UTF8, "text/plain")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new OAuth2AuthProvider(
            "amazon", httpClient, _tokenCache.Object,
            "bad_id", "bad_secret", "https://token.example.com", null, _logger.Object);

        var act = () => provider.GetTokenAsync();
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
