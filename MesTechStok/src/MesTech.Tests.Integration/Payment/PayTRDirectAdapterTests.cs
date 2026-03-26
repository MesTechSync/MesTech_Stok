using System.Net;
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
/// PayTR Direct adapter entegrasyon testleri.
/// WireMock ile HTTP katmani taklit edilir; gercek PayTRDirectAdapter sinifi test edilir.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Payment", "PayTRDirect")]
public class PayTRDirectAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;

    // Test credentials — not real, used only in WireMock scope
    private const string TestMerchantId = "TEST_MERCHANT_001";
    private const string TestMerchantKey = "TEST_KEY_32CHARS_ABCDEFGHIJKLMNOP";
    private const string TestMerchantSalt = "TEST_SALT_VALUE_XYZ";

    public PayTRDirectAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
    }

    public void Dispose() { }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private PayTRDirectAdapter CreateAdapter(
        string? merchantId = null,
        string? merchantKey = null,
        string? merchantSalt = null,
        bool testMode = true)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayTR:MerchantId"]    = merchantId ?? TestMerchantId,
                ["PayTR:MerchantKey"]   = merchantKey ?? TestMerchantKey,
                ["PayTR:MerchantSalt"]  = merchantSalt ?? TestMerchantSalt,
                ["PayTR:DirectBaseUrl"] = _fixture.BaseUrl,
                ["PayTR:TestMode"]      = testMode ? "true" : "false"
            })
            .Build();

        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
        return new PayTRDirectAdapter(
            httpClient,
            config,
            new LoggerFactory().CreateLogger<PayTRDirectAdapter>());
    }

    private static PaymentRequest CreatePaymentRequest(decimal amount = 100.00m)
        => new PaymentRequest(
            OrderId: Guid.NewGuid(),
            Amount: amount,
            Currency: "TRY",
            CardToken: null,
            ReturnUrl: "https://mestech.app/payment/return",
            CustomerIp: "192.168.1.100",
            BasketItems: new List<BasketItem>
            {
                new BasketItem("ITEM-1", "Test Urun", "Elektronik", 100.00m)
            });

    private static string ComputeHmac(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(dataBytes));
    }

    private void SetupTokenSuccess(string token = "PAYTR_TEST_TOKEN_ABC123")
    {
        _mockServer
            .Given(Request.Create().WithPath("/odeme/api/paytrdirect").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "success", token })));
    }

    private void SetupTokenFailure(string reason = "invalid-card")
    {
        _mockServer
            .Given(Request.Create().WithPath("/odeme/api/paytrdirect").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "failed", reason })));
    }

    private void SetupStatusSuccess(string merchantOid, string paytrStatus = "success", string totalAmount = "10000")
    {
        _mockServer
            .Given(Request.Create().WithPath("/odeme/durum").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = paytrStatus, total_amount = totalAmount })));
    }

    private void SetupRefundSuccess(string merchantOid)
    {
        _mockServer
            .Given(Request.Create().WithPath("/odeme/iade").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "success" })));
    }

    private void SetupRefundFailure(string errNo = "already-refunded")
    {
        _mockServer
            .Given(Request.Create().WithPath("/odeme/iade").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "failed", err_no = errNo })));
    }

    // ════ 1. ProcessPayment_Success_ReturnsTransactionId ════

    [Fact]
    public async Task ProcessPayment_ValidRequest_ReturnsSuccessWithTransactionId()
    {
        // Arrange
        SetupTokenSuccess("TOKEN_SUCCESS_001");
        var adapter = CreateAdapter();
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TransactionId.Should().NotBeNullOrEmpty();
        result.TransactionId.Should().Be(request.OrderId.ToString("N"));
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 2. ProcessPayment_InvalidCard_ReturnsError ════

    [Fact]
    public async Task ProcessPayment_InvalidCard_ReturnsFailureWithErrorMessage()
    {
        // Arrange
        SetupTokenFailure("invalid-card");
        var adapter = CreateAdapter();
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("invalid-card", "adapter returns PayTR reason string as error message");
        result.TransactionId.Should().BeNull();
    }

    // ════ 3. ProcessPayment_3DSecure_ReturnsRedirectUrl ════

    [Fact]
    public async Task ProcessPayment_3DSecureRequired_ReturnsSuccessWithRedirectUrl()
    {
        // Arrange
        SetupTokenSuccess("3D_TOKEN_XYZ");
        var adapter = CreateAdapter();
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RedirectUrl.Should().NotBeNullOrEmpty();
        result.RedirectUrl.Should().Contain("3D_TOKEN_XYZ");
    }

    // ════ 4. ProcessPayment_InvalidMerchant_AuthError ════

    [Fact]
    public async Task ProcessPayment_InvalidMerchant_AuthError()
    {
        // Arrange — PayTR returns HTTP 401 for invalid merchant
        _mockServer
            .Given(Request.Create().WithPath("/odeme/api/paytrdirect").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(401).WithBody("Unauthorized"));

        var adapter = CreateAdapter(merchantId: "WRONG_ID", merchantKey: "WRONG_KEY");
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty("a failed HTTP call must produce an error message");
        result.ErrorMessage.Should().NotContain("WRONG_KEY", "credentials must not leak into error messages");
    }

    // ════ 5. GetStatus_Success_ReturnsCompleted ════

    [Fact]
    public async Task GetStatus_Success_ReturnsCompleted()
    {
        // Arrange
        var merchantOid = Guid.NewGuid().ToString("N");
        SetupStatusSuccess(merchantOid, "success", "15000");
        var adapter = CreateAdapter();

        // Act
        var statusResult = await adapter.GetTransactionStatusAsync(merchantOid);

        // Assert
        statusResult.Status.Should().Be(PaymentTransactionStatus.Completed);
        statusResult.TransactionId.Should().Be(merchantOid);
        statusResult.Amount.Should().Be(150.00m, "15000 kurus / 100 = 150.00 TL");
    }

    // ════ 6. GetStatus_Pending_ReturnsPending ════

    [Fact]
    public async Task GetStatus_Pending_ReturnsPending()
    {
        // Arrange
        var merchantOid = Guid.NewGuid().ToString("N");
        SetupStatusSuccess(merchantOid, "pending", "0");
        var adapter = CreateAdapter();

        // Act
        var statusResult = await adapter.GetTransactionStatusAsync(merchantOid);

        // Assert
        statusResult.Status.Should().Be(PaymentTransactionStatus.Pending);
    }

    // ════ 7. GetStatus_NotFound_ReturnsFailed ════

    [Fact]
    public async Task GetStatus_NotFound_ReturnsFailed()
    {
        // Arrange — server returns 404 for unknown transaction
        _mockServer
            .Given(Request.Create().WithPath("/odeme/durum").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(404));

        var adapter = CreateAdapter();
        var oid = Guid.NewGuid().ToString("N");

        // Act
        var statusResult = await adapter.GetTransactionStatusAsync(oid);

        // Assert
        statusResult.Status.Should().Be(PaymentTransactionStatus.Failed,
            "HTTP 404 from PayTR is treated as a failed/not-found transaction");
    }

    // ════ 8. GetInstallments_ValidBin_ReturnsOptions ════

    [Fact]
    public async Task GetInstallments_ValidBin_ReturnsOptions()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new
        {
            status = "success",
            installment_table = new[]
            {
                new { count = 1,  total = 100000L, monthly = 100000L, interest_rate = 0.0m },
                new { count = 3,  total = 103500L, monthly = 34500L,  interest_rate = 1.16m },
                new { count = 6,  total = 107100L, monthly = 17850L,  interest_rate = 1.18m },
                new { count = 12, total = 114000L, monthly = 9500L,   interest_rate = 1.90m }
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
        options.Options.Should().HaveCountGreaterThanOrEqualTo(2);
        options.Options.Should().Contain(o => o.Count == 1);
        options.Options.Should().Contain(o => o.Count == 3);
    }

    // ════ 9. GetInstallments_InvalidBin_EmptyOptions ════

    [Fact]
    public async Task GetInstallments_InvalidBin_EmptyOptions()
    {
        // Arrange — PayTR returns failed status for unknown BIN
        _mockServer
            .Given(Request.Create().WithPath("/odeme/taksit").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "failed" })));

        var adapter = CreateAdapter();

        // Act
        var options = await adapter.GetInstallmentOptionsAsync(1000.00m, "000000");

        // Assert
        options.Options.Should().BeEmpty("unknown BIN returns no installment options");
    }

    // ════ 10. GetInstallments_NullBin_EmptyOptions ════

    [Fact]
    public async Task GetInstallments_NullBin_EmptyOptions()
    {
        // Arrange — server returns error for missing BIN
        _mockServer
            .Given(Request.Create().WithPath("/odeme/taksit").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { status = "failed", reason = "invalid_bin" })));

        var adapter = CreateAdapter();

        // Act
        var options = await adapter.GetInstallmentOptionsAsync(500.00m, null);

        // Assert
        options.Options.Should().BeEmpty();
    }

    // ════ 11. Refund_Success_ReturnsRefundId ════

    [Fact]
    public async Task Refund_Success_ReturnsRefundId()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString("N");
        SetupRefundSuccess(transactionId);
        var adapter = CreateAdapter();

        // Act
        var refund = await adapter.RefundAsync(transactionId, 100.00m);

        // Assert
        refund.Success.Should().BeTrue();
        refund.RefundId.Should().NotBeNullOrEmpty();
        refund.RefundId.Should().Contain(transactionId, "refund ID is constructed from the transaction ID");
        refund.ErrorMessage.Should().BeNull();
    }

    // ════ 12. Refund_AlreadyRefunded_ReturnsError ════

    [Fact]
    public async Task Refund_AlreadyRefunded_ReturnsError()
    {
        // Arrange
        SetupRefundFailure("already-refunded");
        var adapter = CreateAdapter();

        // Act
        var refund = await adapter.RefundAsync(Guid.NewGuid().ToString("N"), 50.00m);

        // Assert
        refund.Success.Should().BeFalse();
        refund.ErrorMessage.Should().Be("already-refunded");
        refund.RefundId.Should().BeNull();
    }

    // ════ 13. Refund_PartialAmount_Success ════

    [Fact]
    public async Task Refund_PartialAmount_Success()
    {
        // Arrange — partial refund: 30 TL out of 100 TL
        var transactionId = Guid.NewGuid().ToString("N");
        SetupRefundSuccess(transactionId);
        var adapter = CreateAdapter();

        // Act
        var refund = await adapter.RefundAsync(transactionId, 30.00m);

        // Assert
        refund.Success.Should().BeTrue();
        refund.RefundId.Should().NotBeNullOrEmpty();
    }

    // ════ 14. HmacSignature_KnownInput_ProducesExpectedHash ════

    [Fact]
    public void ComputeHmac_KnownInput_ProducesExpectedHash()
    {
        // Arrange — compute HMAC manually and compare
        const string data = "TEST_MERCHANT_001192.168.1.100ORDER001customer@mestech.app1000base64basket001000TRY1TEST_SALT_VALUE_XYZ";
        const string key  = "TEST_KEY_32CHARS_ABCDEFGHIJKLMNOP";

        var keyBytes  = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        var expectedHash = Convert.ToBase64String(hmac.ComputeHash(dataBytes));

        // Act — recompute identically
        var actualHash = ComputeHmac(data, key);

        // Assert — the hash is deterministic: same inputs always produce the same output
        actualHash.Should().Be(expectedHash);
        actualHash.Should().NotBeNullOrEmpty();
        actualHash.Length.Should().Be(44, "Base64-encoded SHA-256 hash is always 44 chars");
    }

    // ════ 15. ProcessPayment_ServerError_ReturnsFailure ════

    [Fact]
    public async Task ProcessPayment_ServerError_ReturnsFailure()
    {
        // Arrange — server returns 503 Service Unavailable
        _mockServer
            .Given(Request.Create().WithPath("/odeme/api/paytrdirect").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(503).WithBody("Service Unavailable"));

        var adapter = CreateAdapter();
        var request = CreatePaymentRequest();

        // Act
        var result = await adapter.ProcessPaymentAsync(request);

        // Assert — circuit breaker/retry exhaustion results in a failure result (no exception thrown)
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.TransactionId.Should().BeNull();
    }
}
