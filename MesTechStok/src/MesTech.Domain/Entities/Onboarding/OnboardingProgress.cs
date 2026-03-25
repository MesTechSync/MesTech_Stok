using MesTech.Domain.Common;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities.Onboarding;

/// <summary>
/// Tenant onboarding ilerleme takibi.
/// Adimlar: 1.Kayit → 2.Firma Bilgileri → 3.Ilk Magaza → 4.Platform Bagla →
/// 5.Credential Gir → 6.Ilk Sync → 7.Dashboard
/// </summary>
public sealed class OnboardingProgress : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public OnboardingStep CurrentStep { get; private set; }
    public string? CompletedStepsJson { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsCompleted { get; private set; }

    private OnboardingProgress() { }

    public static OnboardingProgress Start(Guid tenantId)
    {
        return new OnboardingProgress
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CurrentStep = OnboardingStep.Registration,
            CompletedStepsJson = "[]",
            StartedAt = DateTime.UtcNow,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Mevcut adimi tamamla, sonrakine gec.</summary>
    public void CompleteCurrentStep()
    {
        if (IsCompleted)
            throw new InvalidOperationException("Onboarding zaten tamamlandi.");

        // Tamamlanan adimlari JSON'a ekle
        var completed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
            CompletedStepsJson ?? "[]") ?? new List<string>();
        completed.Add(CurrentStep.ToString());
        CompletedStepsJson = System.Text.Json.JsonSerializer.Serialize(completed);

        // Sonraki adima gec
        if (CurrentStep == OnboardingStep.Dashboard)
        {
            IsCompleted = true;
            CompletedAt = DateTime.UtcNow;
            RaiseDomainEvent(new OnboardingCompletedEvent(TenantId, Id, StartedAt, CompletedAt.Value, DateTime.UtcNow));
        }
        else
        {
            CurrentStep = (OnboardingStep)((int)CurrentStep + 1);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Belirli bir adima atla (admin kullanimi).</summary>
    public void SkipToStep(OnboardingStep step)
    {
        if (IsCompleted)
            throw new InvalidOperationException("Onboarding zaten tamamlandi.");

        CurrentStep = step;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Tamamlanma yuzdesi.</summary>
    public int CompletionPercentage =>
        IsCompleted ? 100 : (int)CurrentStep * 100 / 7;
}
