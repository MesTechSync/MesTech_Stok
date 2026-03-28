using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Billing.Commands.CancelSubscription;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;
using MesTech.Application.Features.Billing.Commands.CreateSubscription;
using MesTech.Application.Features.Billing.Commands.ProcessPaymentWebhook;
using MesTech.Application.Features.Billing.Queries.GetBillingInvoices;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;
using MesTech.Application.Features.Billing.Queries.GetUserFeatures;
using MesTech.Application.Features.Billing.Queries.GetTenantSubscription;

namespace MesTech.WebApi.Endpoints;

public static class BillingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/billing")
            .WithTags("Billing")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/billing/plans — list subscription plans
        group.MapGet("/plans", async (
            ISender mediator,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetSubscriptionPlansQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetSubscriptionPlans")
        .WithSummary("Abonelik planları listesi").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/billing/subscription — current tenant subscription
        group.MapGet("/subscription", async (
            ISender mediator,
            Guid tenantId,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetTenantSubscriptionQuery(tenantId), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetTenantSubscription")
        .WithSummary("Mevcut tenant abonelik bilgisi").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/billing/subscription — create subscription
        group.MapPost("/subscription", async (
            ISender mediator,
            CreateSubscriptionCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/billing/subscription", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateSubscription")
        .WithSummary("Yeni abonelik başlat")
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/billing/subscription/cancel — cancel subscription
        group.MapPost("/subscription/cancel", async (
            ISender mediator,
            CancelSubscriptionCommand command,
            CancellationToken ct = default) =>
        {
            await mediator.Send(command, ct);
            return Results.NoContent();
        })
        .WithName("CancelSubscription")
        .WithSummary("Abonelik iptal et").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/billing/invoices — billing invoices
        group.MapGet("/invoices", async (
            ISender mediator,
            Guid tenantId,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetBillingInvoicesQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetBillingInvoices")
        .WithSummary("Faturalama geçmişi").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/billing/invoices — create billing invoice
        group.MapPost("/invoices", async (
            ISender mediator,
            CreateBillingInvoiceCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/billing/invoices/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateBillingInvoice")
        .WithSummary("Fatura oluştur")
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/billing/subscription/change-plan — plan yükselt/düşür
        group.MapPut("/subscription/change-plan", async (
            ISender mediator,
            ChangeSubscriptionPlanCommand command,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ChangeSubscriptionPlan")
        .WithSummary("Abonelik planını değiştir (upgrade/downgrade)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/billing/usage — plan kullanım durumu
        group.MapGet("/usage", async (
            ISender mediator,
            Guid tenantId,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetSubscriptionUsageQuery(tenantId), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetSubscriptionUsage")
        .WithSummary("Abonelik kullanım durumu (store/product/user limitleri)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/billing/webhooks/{provider} — payment webhook receiver
        group.MapPost("/webhooks/{provider}", async (
            string provider,
            HttpContext httpContext,
            ISender mediator,
            CancellationToken ct = default) =>
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            if (string.IsNullOrWhiteSpace(body))
                return Results.BadRequest(ApiResponse<object>.Fail("Empty webhook body", "EMPTY_BODY"));

            var signature = httpContext.Request.Headers["Stripe-Signature"].FirstOrDefault()
                         ?? httpContext.Request.Headers["X-Webhook-Signature"].FirstOrDefault();

            var result = await mediator.Send(
                new ProcessPaymentWebhookCommand(provider, body, signature), ct);

            return result.Success
                ? Results.Ok(ApiResponse<PaymentWebhookResult>.Ok(result))
                : Results.UnprocessableEntity(ApiResponse<PaymentWebhookResult>.Fail(result.Error ?? "Webhook processing failed"));
        })
        .WithName("ProcessPaymentWebhook")
        .WithSummary("Payment provider webhook receiver (Stripe/Iyzico)")
        .AllowAnonymous() // Webhook'lar JWT olmadan gelir
        .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(1_048_576)); // G088: 1MB limit

        // GET /api/v1/billing/features — tenant feature flags (plan bazlı)
        group.MapGet("/features", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetUserFeaturesQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetUserFeatures")
        .WithSummary("Tenant aktif özellik listesi — plan bazlı feature flags")
        .Produces(200)
        .CacheOutput("Lookup60s");
    }
}
