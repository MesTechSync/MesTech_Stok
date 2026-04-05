using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.WebApi.Filters;

/// <summary>
/// Plan limit kontrol filtresi — product/store oluşturma endpoint'lerine uygulanır.
/// Limit aşılırsa 403 Forbidden + upgrade mesajı döner.
/// G134 FIX: Distributed lock ile count+create atomik — TOCTOU race condition önlendi.
/// </summary>
public sealed class ProductPlanLimitFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var checkResult = await CheckLimitAsync(context, "products").ConfigureAwait(false);
        if (checkResult is not null) return checkResult;

        try
        {
            return await next(context).ConfigureAwait(false);
        }
        finally
        {
            // G134: Release distributed lock after handler completes
            if (context.HttpContext.Items.TryGetValue("__PlanLimitLock", out var lockObj)
                && lockObj is IAsyncDisposable lockHandle)
            {
                await lockHandle.DisposeAsync().ConfigureAwait(false);
                context.HttpContext.Items.Remove("__PlanLimitLock");
            }
        }
    }

    internal static async Task<IResult?> CheckLimitAsync(EndpointFilterInvocationContext context, string resource)
    {
        var services = context.HttpContext.RequestServices;
        var logger = services.GetRequiredService<ILogger<ProductPlanLimitFilter>>();

        var tenantIdStr = context.HttpContext.Request.Query["tenantId"].FirstOrDefault()
            ?? context.HttpContext.Request.RouteValues["tenantId"]?.ToString();

        if (!Guid.TryParse(tenantIdStr, out var tenantId))
            return null; // No tenant context — skip

        // G134 FIX: Distributed lock — count+create atomik
        // Lock olmadan iki eşzamanlı istek aynı count'u görüp limiti aşabilir
        var lockService = services.GetService<IDistributedLockService>();
        var lockKey = $"plan-limit:{tenantId}:{resource}";
        IAsyncDisposable? lockHandle = null;

        if (lockService is not null)
        {
            lockHandle = await lockService.AcquireLockAsync(
                lockKey, expiry: TimeSpan.FromSeconds(10), waitTimeout: TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            if (lockHandle is null)
            {
                logger.LogWarning("Plan limit lock timeout: tenant={TenantId} resource={Resource}", tenantId, resource);
                return Results.Json(
                    ApiResponse<object>.Fail("Islem sirada, lutfen tekrar deneyin.", "CONCURRENT_LIMIT_CHECK"),
                    statusCode: 429);
            }
        }

        // Lock alındıktan sonra context'e kaydet — handler tamamlandığında release edilecek
        if (lockHandle is not null)
            context.HttpContext.Items["__PlanLimitLock"] = lockHandle;

        var subscriptionRepo = services.GetRequiredService<ITenantSubscriptionRepository>();
        var planRepo = services.GetRequiredService<ISubscriptionPlanRepository>();

        var subscription = await subscriptionRepo.GetActiveByTenantIdAsync(tenantId).ConfigureAwait(false);
        if (subscription is null)
        {
            logger.LogWarning("Plan limit: no active subscription for tenant {TenantId}", tenantId);
            return Results.Json(
                ApiResponse<object>.Fail("Aktif abonelik bulunamadi. Lutfen bir plan secin.", "NO_SUBSCRIPTION"),
                statusCode: 403);
        }

        if (subscription.IsExpired)
        {
            return Results.Json(
                ApiResponse<object>.Fail("Deneme suresiniz doldu. Lutfen bir plan satin alin.", "TRIAL_EXPIRED"),
                statusCode: 403);
        }

        var plan = await planRepo.GetByIdAsync(subscription.PlanId).ConfigureAwait(false);
        if (plan is null) return null;

        int currentCount;
        int limit;

        switch (resource)
        {
            case "products":
                var productRepo = services.GetRequiredService<IProductRepository>();
                currentCount = await productRepo.CountByTenantAsync(tenantId).ConfigureAwait(false);
                limit = plan.MaxProducts;
                break;
            case "stores":
                var storeRepo = services.GetRequiredService<IStoreRepository>();
                currentCount = await storeRepo.CountByTenantAsync(tenantId).ConfigureAwait(false);
                limit = plan.MaxStores;
                break;
            case "users":
                var userRepo = services.GetRequiredService<IUserRepository>();
                currentCount = await userRepo.CountByTenantAsync(tenantId).ConfigureAwait(false);
                limit = plan.MaxUsers;
                break;
            default:
                return null;
        }

        if (currentCount >= limit)
        {
            logger.LogWarning("Plan limit exceeded: tenant {TenantId}, {Resource} {Current}/{Limit}",
                tenantId, resource, currentCount, limit);
            return Results.Json(
                ApiResponse<object>.Fail(
                    $"{resource} limiti asildi ({currentCount}/{limit}). Planınızı yükseltin.",
                    "PLAN_LIMIT_EXCEEDED"),
                statusCode: 403);
        }

        return null;
    }
}

/// <summary>Store oluşturma limit filtresi.</summary>
public sealed class StorePlanLimitFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var checkResult = await ProductPlanLimitFilter.CheckLimitAsync(context, "stores").ConfigureAwait(false);
        if (checkResult is not null) return checkResult;

        try { return await next(context).ConfigureAwait(false); }
        finally
        {
            if (context.HttpContext.Items.TryGetValue("__PlanLimitLock", out var l) && l is IAsyncDisposable lh)
            { await lh.DisposeAsync().ConfigureAwait(false); context.HttpContext.Items.Remove("__PlanLimitLock"); }
        }
    }
}

/// <summary>User oluşturma limit filtresi — MaxUsers plan limiti.</summary>
public sealed class UserPlanLimitFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var checkResult = await ProductPlanLimitFilter.CheckLimitAsync(context, "users").ConfigureAwait(false);
        if (checkResult is not null) return checkResult;

        try { return await next(context).ConfigureAwait(false); }
        finally
        {
            if (context.HttpContext.Items.TryGetValue("__PlanLimitLock", out var l) && l is IAsyncDisposable lh)
            { await lh.DisposeAsync().ConfigureAwait(false); context.HttpContext.Items.Remove("__PlanLimitLock"); }
        }
    }
}

/// <summary>
/// Feature gate middleware — plan FeaturesJson'a göre endpoint erişim kontrolü.
/// Kilitli özelliğe erişim → 402 Payment Required + plan önerisi.
/// Endpoint'e .AddEndpointFilter(new FeatureGateFilter("feature_name")) şeklinde uygulanır.
/// </summary>
public sealed class FeatureGateFilter : IEndpointFilter
{
    private readonly string _requiredFeature;

    public FeatureGateFilter(string requiredFeature)
        => _requiredFeature = requiredFeature;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var logger = services.GetRequiredService<ILogger<FeatureGateFilter>>();

        var tenantIdStr = context.HttpContext.Request.Query["tenantId"].FirstOrDefault()
            ?? context.HttpContext.Request.RouteValues["tenantId"]?.ToString();

        if (!Guid.TryParse(tenantIdStr, out var tenantId))
            return await next(context).ConfigureAwait(false);

        var subscriptionRepo = services.GetRequiredService<ITenantSubscriptionRepository>();
        var planRepo = services.GetRequiredService<ISubscriptionPlanRepository>();

        var subscription = await subscriptionRepo.GetActiveByTenantIdAsync(tenantId).ConfigureAwait(false);
        if (subscription is null)
        {
            return Results.Json(
                ApiResponse<object>.Fail(
                    "Bu ozellik icin aktif abonelik gerekli.",
                    "SUBSCRIPTION_REQUIRED"),
                statusCode: 402);
        }

        var plan = await planRepo.GetByIdAsync(subscription.PlanId).ConfigureAwait(false);
        if (plan is null)
            return await next(context).ConfigureAwait(false);

        // Check FeaturesJson for the required feature — proper JSON parsing (FMEA-DEV6)
        if (!string.IsNullOrWhiteSpace(plan.FeaturesJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(plan.FeaturesJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(prop.Name, _requiredFeature, StringComparison.OrdinalIgnoreCase)
                        && (prop.Value.ValueKind == System.Text.Json.JsonValueKind.True
                            || (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String
                                && string.Equals(prop.Value.GetString(), "true", StringComparison.OrdinalIgnoreCase))))
                    {
                        return await next(context).ConfigureAwait(false);
                    }
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogWarning(ex, "Feature gate: invalid FeaturesJson for plan {PlanId}", plan.Id);
            }
        }

        logger.LogInformation(
            "Feature gate blocked: Tenant={TenantId} Feature={Feature} Plan={Plan}",
            tenantId, _requiredFeature, plan.Name);

        return Results.Json(
            ApiResponse<object>.Fail(
                $"'{_requiredFeature}' ozelligi mevcut planinizda bulunmuyor. Planınızı yükseltin.",
                "FEATURE_NOT_AVAILABLE"),
            statusCode: 402);
    }
}
