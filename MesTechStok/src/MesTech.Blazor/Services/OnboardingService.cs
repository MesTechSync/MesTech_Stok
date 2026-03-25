namespace MesTech.Blazor.Services;

/// <summary>
/// Onboarding service — API entegrasyonlu, fallback destekli.
/// API erişilemezse varsayılan adımlar gösterilir.
/// </summary>
public class OnboardingService
{
    private readonly MesTechApiClient _apiClient;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(MesTechApiClient apiClient, ILogger<OnboardingService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<bool> IsOnboardingCompletedAsync(Guid userId)
    {
        var result = await _apiClient.SafeGetAsync<OnboardingStatusDto>(
            $"system/onboarding/status?userId={userId}");

        if (result.IsSuccess && result.Data is not null)
            return result.Data.IsCompleted;

        // API erişilemez → mevcut kullanıcıları rahatsız etme
        _logger.LogDebug("Onboarding status API erişilemedi — varsayılan: tamamlanmış");
        return true;
    }

    public async Task MarkOnboardingCompletedAsync(Guid userId)
    {
        var result = await _apiClient.SafePostAsync<object>(
            $"system/onboarding/complete?userId={userId}", new { UserId = userId });

        if (!result.IsSuccess)
            _logger.LogWarning("Onboarding tamamlama API'ye iletilemedi — userId={UserId}", userId);
    }

    public async Task SaveOnboardingStepAsync(Guid userId, int step, Dictionary<string, string>? data = null)
    {
        var result = await _apiClient.SafePostAsync<object>(
            "system/onboarding/step",
            new { UserId = userId, Step = step, Data = data });

        if (!result.IsSuccess)
            _logger.LogWarning("Onboarding adım {Step} kaydedilemedi — userId={UserId}", step, userId);
    }

    public async Task<int> GetLastCompletedStepAsync(Guid userId)
    {
        var result = await _apiClient.SafeGetAsync<OnboardingProgressDto>(
            $"system/onboarding/progress?userId={userId}");

        if (result.IsSuccess && result.Data is not null)
            return result.Data.LastCompletedStep;

        return 0; // Fallback: baştan başla
    }
    public async Task<RegisterTenantResultDto?> RegisterTenantAsync(RegisterTenantRequestDto request)
    {
        var result = await _apiClient.SafePostAsync<RegisterTenantResultDto>(
            "onboarding/register", request);

        if (result.IsSuccess && result.Data is not null)
            return result.Data;

        _logger.LogWarning("Tenant kayıt API'ye iletilemedi — companyName={CompanyName}", request.CompanyName);
        return null;
    }

    public async Task SaveOnboardingDataAsync(Guid userId, int step, Dictionary<string, string> data)
    {
        var result = await _apiClient.SafePostAsync<object>(
            "onboarding/complete-step",
            new { TenantId = userId, StepNumber = step, StepData = data });

        if (!result.IsSuccess)
            _logger.LogWarning("Onboarding data adım {Step} kaydedilemedi — userId={UserId}", step, userId);
    }
}

// ── DTOs ──
public record OnboardingStatusDto(bool IsCompleted, int CompletedSteps, int TotalSteps);
public record OnboardingProgressDto(int LastCompletedStep, int TotalSteps);

public record RegisterTenantRequestDto(
    string CompanyName,
    string? TaxNumber,
    string AdminUsername,
    string AdminEmail,
    string AdminPassword,
    string? AdminFirstName = null,
    string? AdminLastName = null);

public record RegisterTenantResultDto(
    Guid TenantId,
    Guid AdminUserId,
    Guid SubscriptionId,
    Guid OnboardingId,
    DateTime TrialEndsAt,
    string PlanName);
