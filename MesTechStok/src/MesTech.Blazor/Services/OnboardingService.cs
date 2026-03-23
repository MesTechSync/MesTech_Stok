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
}

// ── DTOs ──
public record OnboardingStatusDto(bool IsCompleted, int CompletedSteps, int TotalSteps);
public record OnboardingProgressDto(int LastCompletedStep, int TotalSteps);
