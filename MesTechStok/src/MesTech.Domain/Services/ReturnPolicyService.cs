using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// İade politika servisi — platform kurallarını uygular.
/// Saf domain servisi, altyapı bağımlılığı yok.
/// </summary>
public class ReturnPolicyService
{
    private readonly IReadOnlyDictionary<PlatformType, PlatformReturnPolicy> _policies;

    public ReturnPolicyService()
        : this(PlatformReturnPolicy.Defaults) { }

    public ReturnPolicyService(IReadOnlyDictionary<PlatformType, PlatformReturnPolicy> policies)
    {
        _policies = policies;
    }

    /// <summary>
    /// İade talebini platform kurallarına göre doğrular.
    /// </summary>
    public ReturnValidationResult Validate(ReturnRequest request, Order order)
    {
        var policy = GetPolicy(request.Platform);

        // Süre kontrolü
        var daysSinceOrder = (request.RequestDate - order.OrderDate).TotalDays;
        if (daysSinceOrder > policy.ReturnWindowDays)
        {
            return ReturnValidationResult.Fail(
                $"İade süresi dolmuş: {policy.ReturnWindowDays} gün ({daysSinceOrder:N0} gün geçti)");
        }

        // Sipariş durumu kontrolü
        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Shipped)
        {
            return ReturnValidationResult.Fail(
                $"Sipariş iade edilebilir durumda değil: {order.Status}");
        }

        return ReturnValidationResult.Success(policy);
    }

    /// <summary>
    /// İade talebine platform kurallarını uygular.
    /// </summary>
    public void ApplyPolicy(ReturnRequest request)
    {
        var policy = GetPolicy(request.Platform);

        request.IsCargoFree = policy.IsCargoFree;
        request.DeadlineDate = request.RequestDate.AddDays(policy.ReturnWindowDays);

        if (!policy.RequiresApproval)
            request.Approve();
    }

    /// <summary>
    /// Otomatik stok iadesi yapılmalı mı?
    /// </summary>
    public bool ShouldAutoRestoreStock(PlatformType platform)
    {
        return GetPolicy(platform).AutoRestoreStock;
    }

    public PlatformReturnPolicy GetPolicy(PlatformType platform)
    {
        return _policies.TryGetValue(platform, out var policy)
            ? policy
            : new PlatformReturnPolicy
            {
                Platform = platform,
                ReturnWindowDays = 14,
                IsCargoFree = false,
                RequiresApproval = true,
                AutoRestoreStock = true
            };
    }
}

/// <summary>
/// İade doğrulama sonucu.
/// </summary>
public record ReturnValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public PlatformReturnPolicy? Policy { get; init; }

    public static ReturnValidationResult Success(PlatformReturnPolicy policy) =>
        new() { IsValid = true, Policy = policy };

    public static ReturnValidationResult Fail(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}
