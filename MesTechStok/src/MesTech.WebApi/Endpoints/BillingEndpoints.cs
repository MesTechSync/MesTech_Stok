using MediatR;
using MesTech.Application.Features.Billing.Commands.CancelSubscription;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;
using MesTech.Application.Features.Billing.Commands.CreateSubscription;
using MesTech.Application.Features.Billing.Queries.GetBillingInvoices;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;
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
        .WithSummary("Abonelik planları listesi");

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
        .WithSummary("Mevcut tenant abonelik bilgisi");

        // POST /api/v1/billing/subscription — create subscription
        group.MapPost("/subscription", async (
            ISender mediator,
            CreateSubscriptionCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/billing/subscription", new { id });
        })
        .WithName("CreateSubscription")
        .WithSummary("Yeni abonelik başlat");

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
        .WithSummary("Abonelik iptal et");

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
        .WithSummary("Faturalama geçmişi");

        // POST /api/v1/billing/invoices — create billing invoice
        group.MapPost("/invoices", async (
            ISender mediator,
            CreateBillingInvoiceCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/billing/invoices/{id}", new { id });
        })
        .WithName("CreateBillingInvoice")
        .WithSummary("Fatura oluştur");

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
        .WithSummary("Abonelik planını değiştir (upgrade/downgrade)");

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
        .WithSummary("Abonelik kullanım durumu (store/product/user limitleri)");
    }
}
