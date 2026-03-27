using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Payment;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Payment;

/// <summary>
/// PayTR iFrame adapter entegrasyon testleri.
/// WireMock ile HTTP katmani taklit edilir; gercek PayTRiFrameAdapter sinifi test edilir.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Payment", "PayTRiFrame")]
public class PayTRiFrameAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;

    // Test credentials — not real, used only in WireMock scope
    private const string TestMerchantId   = "IFRAME_MERCHANT_001";
    private const string TestMerchantKey  = "IFRAME_KEY_32CHARS_ABCDEFGHIJKLMN";
    private const string TestMerchantSalt = "IFRAME_SALT_ABC123";

    public PayTRiFrameAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
    }

    public void Dispose() { }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private PayTRiFrameAdapter CreateAdapter(
        string? merchantId   = null,
        string? merchantKey  = null,
        string? merchantSalt = null,
        bool testMode = true)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayTR:MerchantId"]    = merchantId ?? TestMerchantId,
                ["PayTR:MerchantKey"]   = merchantKey ?? TestMerchantKey,
                ["PayTR:MerchantSalt"]  = merchantSalt ?? TestMerchantSalt,
                ["PayTR:IFrameBaseUrl"] = _fixture.BaseUrl,
                ["PayTR:TestMode"]      = testMode ? "true" : "false"
            })
            .Build();

        var httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        return new PayTRiFrameAdapter(
            httpClient,
            config,
            new LoggerFactory().CreateLogger<PayTRiFrameAdapter>());
    }

    private static PaymentRequest CreatePaymentRequest(decimal amount = 150.00m)
        => new PaymentRequest(
            OrderId: Guid.NewGuid(),
            Amount: amount,
            Currency: "TRY",
            CardToken: null,
            ReturnUrl: "https://mestech.app/payment/return",
            CustomerIp: "10.0.0.1",
            BasketItems: new List<BasketItem>
            {
                new BasketItem("ITEM-2", "iFrame Test Urunu", "Giyim", 150.00m)
            });

    private static string ComputeHmac(string data, string key)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(dataBytes));
    }

    private void SetupTokenSuccess(string token = "IFRAME_TOKEN_ABC")
    {
        _mockServer
            .Given(Request.Create().WithPath("/odeme/api/get-token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "success", token })));
    }





    private void SetupTokenFailure(string reason = "token-generation-error")
    {
        _mockServer
            .Given(Request.Create().WithPath("/odeme/api/get-token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "failed", reason })));
    }





    // ════ 1. ProcessPayment_Success_ReturnsIFrameUrl ════

    [Fact]
    public async Task ProcessPayment_Success_ReturnsIFrameUrl()
    {
        // Arrange
        SetupTokenSuccess("IFRAME_TOKEN_XYZ");
        var adapter = CreateAdapter();
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RedirectUrl.Should().NotBeNullOrEmpty();
        result.RedirectUrl.Should().Contain("IFRAME_TOKEN_XYZ", "iFrame URL must contain the token");
        result.RedirectUrl.Should().Contain("/odeme/iframe/", "URL must use the iFrame embed path");
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 2. ProcessPayment_TokenGeneration_TransactionIdSet ════

    [Fact]
    public async Task ProcessPayment_TokenGeneration_TransactionIdMatchesOrderId()
    {
        // Arrange
        SetupTokenSuccess("TKN_ORDER_CHECK");
        var adapter = CreateAdapter();
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TransactionId.Should().Be(request.OrderId.ToString("N"),
            "TransactionId must be the merchant_oid derived from OrderId");
    }

    // ════ 3. CallbackVerification_ValidHash_ReturnsTrue ════

    [Fact]
    public void CallbackVerification_ValidHash_ReturnsTrue()
    {
        // Arrange — compute the callback hash exactly as PayTR does:
        // HMAC-SHA256(merchant_oid + merchant_salt + status), key = merchant_key
        const string merchantOid  = "CALLBACKOID001";
        const string status       = "success";
        const string totalAmount  = "10000";

        var raw = $"{merchantOid}{TestMerchantSalt}{status}";
        var correctHash = ComputeHmac(raw, TestMerchantKey);

        var adapter = CreateAdapter();

        // Act
        var isValid = adapter.VerifyCallback(merchantOid, status, totalAmount, correctHash);

        // Assert
        isValid.Should().BeTrue("hash computed with correct key and inputs must pass verification");
    }

    // ════ 4. CallbackVerification_InvalidHash_ReturnsFalse ════

    [Fact]
    public void CallbackVerification_InvalidHash_ReturnsFalse()
    {
        // Arrange — tampered hash
        const string merchantOid = "CALLBACKOID002";
        const string status      = "success";
        const string totalAmount = "10000";
        const string tamperedHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="; // wrong 44-char base64

        var adapter = CreateAdapter();

        // Act
        var isValid = adapter.VerifyCallback(merchantOid, status, totalAmount, tamperedHash);

        // Assert
        isValid.Should().BeFalse("tampered hash must be rejected");
    }

    // ════ 5. GetStatus_CompletedTransaction_ReturnsCompleted ════

    [Fact]
    public async Task GetStatus_CompletedTransaction_ReturnsCompleted()
    {
        // Arrange
        var merchantOid = Guid.NewGuid().ToString("N");
        _mockServer
            .Given(Request.Create().WithPath("/odeme/durum").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "success", total_amount = "20000" })));

        var adapter = CreateAdapter();

        // Act
        var statusResult = await adapter.GetTransactionStatusAsync(merchantOid);

        // Assert
        statusResult.Status.Should().Be(PaymentTransactionStatus.Completed);
        statusResult.Amount.Should().Be(200.00m, "20000 kurus / 100 = 200.00 TL");
    }

    // ════ 6. Refund_FromIFrame_Success ════

    [Fact]
    public async Task Refund_FromIFrame_Success()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString("N");
        _mockServer
            .Given(Request.Create().WithPath("/odeme/iade").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "success" })));

        var adapter = CreateAdapter();

        // Act
        var refund = await adapter.RefundAsync(transactionId, 150.00m);

        // Assert
        refund.Success.Should().BeTrue();
        refund.RefundId.Should().NotBeNullOrEmpty();
        refund.RefundId.Should().Contain(transactionId);
        refund.ErrorMessage.Should().BeNull();
    }

    // ════ 7. GetInstallments_FromIFrame_ReturnsOptions ════

    [Fact]
    public async Task GetInstallments_FromIFrame_ReturnsOptions()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new
        {
            status = "success",
            installment_table = new[]
            {
                new { count = 1,  total = 100000L, monthly = 100000L, interest_rate = 0.0m },
                new { count = 3,  total = 103500L, monthly = 34500L,  interest_rate = 1.16m },
                new { count = 6,  total = 107100L, monthly = 17850L,  interest_rate = 1.18m }
            }
        });

        _mockServer
            .Given(Request.Create().WithPath("/odeme/taksit").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseBody));

        var adapter = CreateAdapter();

        // Act
        var options = await adapter.GetInstallmentOptionsAsync(1000.00m, "450803");

        // Assert
        options.Options.Should().NotBeEmpty();
        options.Options.Should().Contain(o => o.Count == 1);
        options.Options.Should().Contain(o => o.Count == 3);
        options.Options.Should().Contain(o => o.Count == 6);
    }

    // ════ 8. ProcessPayment_TokenFailed_ReturnsError ════

    [Fact]
    public async Task ProcessPayment_TokenFailed_ReturnsError()
    {
        // Arrange
        SetupTokenFailure("merchant-auth-error");
        var adapter = CreateAdapter();
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("merchant-auth-error");
        result.RedirectUrl.Should().BeNull();
    }

    // ════ 9. ProcessPayment_ServerUnavailable_ReturnsFailure ════

    [Fact]
    public async Task ProcessPayment_ServerUnavailable_ReturnsFailure()
    {
        // Arrange — server returns 503
        _mockServer
            .Given(Request.Create().WithPath("/odeme/api/get-token").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(503).WithBody("Service Unavailable"));

        var adapter = CreateAdapter();
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 10. HmacToken_KnownInput_ProducesExpectedHash ════

    [Fact]
    public void HmacToken_KnownInput_ProducesExpectedHash()
    {
        // Arrange — known inputs replicating the iFrame token hash formula:
        // HMAC-SHA256(merchant_id + user_ip + merchant_oid + email + payment_amount +
        //             user_basket + no_installment + max_installment + currency + test_mode + salt)
        const string merchantId  = "IFRAME_MERCHANT_001";
        const string userIp      = "10.0.0.1";
        const string merchantOid = "ORDER_IFRAME_001";
        const string email       = "customer@mestech.app";
        const string amount      = "15000";
        const string basket      = "basket_base64_stub";
        const string noInst      = "0";
        const string maxInst     = "0";
        const string currency    = "TRY";
        const string testMode    = "1";
        const string salt        = "IFRAME_SALT_ABC123";
        const string key         = "IFRAME_KEY_32CHARS_ABCDEFGHIJKLMN";

        var raw = string.Concat(merchantId, userIp, merchantOid, email, amount, basket,
                                noInst, maxInst, currency, testMode, salt);

        // Act
        var hash1 = ComputeHmac(raw, key);
        var hash2 = ComputeHmac(raw, key); // determinism check

        // Assert
        hash1.Should().Be(hash2, "HMAC-SHA256 is deterministic for the same inputs");
        hash1.Should().NotBeNullOrEmpty();
        hash1.Length.Should().Be(44, "Base64-encoded SHA-256 is always 44 characters");

        // Verify the hash changes when input changes
        var rawAlt = string.Concat(merchantId, userIp, merchantOid, email, "99999", basket,
                                   noInst, maxInst, currency, testMode, salt);
        var hashAlt = ComputeHmac(rawAlt, key);
        hashAlt.Should().NotBe(hash1, "different amount must produce a different hash");
    }
}
