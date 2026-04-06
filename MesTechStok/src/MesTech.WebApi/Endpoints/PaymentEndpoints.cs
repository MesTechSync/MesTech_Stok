using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class PaymentEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/payments — ödeme işlemi başlat (provider: PayTRDirect|Stripe|Iyzico)
        group.MapPost("/", async (
            PaymentRequest request,
            PaymentProviderType? provider,
            IEnumerable<IPaymentProvider> providers,
            CancellationToken ct) =>
        {
            var paymentProvider = ResolveProvider(providers, provider ?? PaymentProviderType.PayTRDirect);
            var result = await paymentProvider.ProcessPaymentAsync(request, ct);
            return result.Success
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 422);
        })
        .WithName("InitiatePayment")
        .WithSummary("Ödeme işlemi başlat — provider query param: PayTRDirect, PayTRiFrame, Iyzico, Stripe")
        .Produces(200).Produces(422)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/payments/{transactionId} — işlem durumu sorgula
        group.MapGet("/{transactionId}", async (
            string transactionId,
            PaymentProviderType? provider,
            IEnumerable<IPaymentProvider> providers,
            CancellationToken ct) =>
        {
            var paymentProvider = ResolveProvider(providers, provider ?? PaymentProviderType.PayTRDirect);
            var result = await paymentProvider.GetTransactionStatusAsync(transactionId, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetPaymentStatus")
        .WithSummary("İşlem ID ile ödeme durumunu sorgula").Produces(200).Produces(400);

        // POST /api/v1/payments/{transactionId}/refund — iade işlemi
        group.MapPost("/{transactionId}/refund", async (
            string transactionId,
            RefundRequest request,
            PaymentProviderType? provider,
            IEnumerable<IPaymentProvider> providers,
            CancellationToken ct) =>
        {
            var paymentProvider = ResolveProvider(providers, provider ?? PaymentProviderType.PayTRDirect);
            var result = await paymentProvider.RefundAsync(transactionId, request.Amount, ct);
            return result.Success
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 422);
        })
        .WithName("RefundPayment")
        .WithSummary("Ödeme iadesi başlat")
        .Produces(200).Produces(422)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/payments/installments — taksit seçenekleri sorgula
        group.MapGet("/installments", async (
            decimal amount,
            string? binNumber,
            PaymentProviderType? provider,
            IEnumerable<IPaymentProvider> providers,
            CancellationToken ct) =>
        {
            var paymentProvider = ResolveProvider(providers, provider ?? PaymentProviderType.PayTRDirect);
            var result = await paymentProvider.GetInstallmentOptionsAsync(amount, binNumber, ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetInstallmentOptions")
        .WithSummary("Tutar ve BIN numarasına göre taksit seçeneklerini getir").Produces(200).Produces(400);
    }

    private static IPaymentProvider ResolveProvider(IEnumerable<IPaymentProvider> providers, PaymentProviderType type)
    {
        return providers.FirstOrDefault(p => p.Provider == type)
            ?? throw new InvalidOperationException($"Payment provider '{type}' is not registered.");
    }

    /// <summary>İade istek gövdesi — iade miktarı.</summary>
    public record RefundRequest(decimal Amount);
}
