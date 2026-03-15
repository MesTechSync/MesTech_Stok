using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

public static class PaymentEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/payments — ödeme işlemi başlat
        group.MapPost("/", async (
            PaymentRequest request,
            IPaymentProvider paymentProvider,
            CancellationToken ct) =>
        {
            var result = await paymentProvider.ProcessPaymentAsync(request, ct);
            return result.Success
                ? Results.Ok(result)
                : Results.UnprocessableEntity(new { result.ErrorMessage });
        })
        .WithName("InitiatePayment")
        .WithSummary("Yeni ödeme işlemi başlat (PayTR)");

        // GET /api/v1/payments/{transactionId} — işlem durumu sorgula
        group.MapGet("/{transactionId}", async (
            string transactionId,
            IPaymentProvider paymentProvider,
            CancellationToken ct) =>
        {
            var result = await paymentProvider.GetTransactionStatusAsync(transactionId, ct);
            return Results.Ok(result);
        })
        .WithName("GetPaymentStatus")
        .WithSummary("İşlem ID ile ödeme durumunu sorgula");

        // POST /api/v1/payments/{transactionId}/refund — iade işlemi
        group.MapPost("/{transactionId}/refund", async (
            string transactionId,
            RefundRequest request,
            IPaymentProvider paymentProvider,
            CancellationToken ct) =>
        {
            var result = await paymentProvider.RefundAsync(transactionId, request.Amount, ct);
            return result.Success
                ? Results.Ok(result)
                : Results.UnprocessableEntity(new { result.ErrorMessage });
        })
        .WithName("RefundPayment")
        .WithSummary("Ödeme iadesi başlat");

        // GET /api/v1/payments/installments — taksit seçenekleri sorgula
        group.MapGet("/installments", async (
            decimal amount,
            string? binNumber,
            IPaymentProvider paymentProvider,
            CancellationToken ct) =>
        {
            var result = await paymentProvider.GetInstallmentOptionsAsync(amount, binNumber, ct);
            return Results.Ok(result);
        })
        .WithName("GetInstallmentOptions")
        .WithSummary("Tutar ve BIN numarasına göre taksit seçeneklerini getir");
    }

    /// <summary>İade istek gövdesi — iade miktarı.</summary>
    public record RefundRequest(decimal Amount);
}
