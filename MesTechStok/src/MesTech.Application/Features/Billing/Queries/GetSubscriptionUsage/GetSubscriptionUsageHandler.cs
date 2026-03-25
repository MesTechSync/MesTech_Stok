using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;

public sealed class GetSubscriptionUsageHandler
    : IRequestHandler<GetSubscriptionUsageQuery, SubscriptionUsageDto?>
{
    private readonly ITenantSubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IStoreRepository _storeRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUserRepository _userRepo;

    public GetSubscriptionUsageHandler(
        ITenantSubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        IStoreRepository storeRepo,
        IProductRepository productRepo,
        IUserRepository userRepo)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _storeRepo = storeRepo;
        _productRepo = productRepo;
        _userRepo = userRepo;
    }

    public async Task<SubscriptionUsageDto?> Handle(
        GetSubscriptionUsageQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepo
            .GetActiveByTenantIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
            return null;

        var plan = await _planRepo
            .GetByIdAsync(subscription.PlanId, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
            return null;

        var storesUsed = await _storeRepo
            .CountByTenantAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        var productsUsed = await _productRepo
            .CountByTenantAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        // User count: tüm kullanıcılar (User entity tenant-agnostic)
        var allUsers = await _userRepo
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);
        var usersUsed = allUsers.Count;

        var maxUsage = Math.Max(
            Math.Max(
                plan.MaxStores > 0 ? (decimal)storesUsed / plan.MaxStores * 100 : 0,
                plan.MaxProducts > 0 ? (decimal)productsUsed / plan.MaxProducts * 100 : 0),
            plan.MaxUsers > 0 ? (decimal)usersUsed / plan.MaxUsers * 100 : 0);

        return new SubscriptionUsageDto
        {
            PlanName = plan.Name,
            Status = subscription.Status.ToString(),
            TrialEndsAt = subscription.TrialEndsAt,
            NextBillingDate = subscription.NextBillingDate,
            StoresUsed = storesUsed,
            StoresLimit = plan.MaxStores,
            ProductsUsed = productsUsed,
            ProductsLimit = plan.MaxProducts,
            UsersUsed = usersUsed,
            UsersLimit = plan.MaxUsers,
            UsagePercent = Math.Round(maxUsage, 1),
            IsOverLimit = storesUsed > plan.MaxStores
                       || productsUsed > plan.MaxProducts
                       || usersUsed > plan.MaxUsers
        };
    }
}
