using MediatR;

namespace MesTech.Application.Features.Onboarding.Queries.GetV5ReadinessCheck;

/// <summary>
/// V5 özellik hazırlık kontrolü — Fulfillment, ERP, Raporlama, Bildirim kurulumu durumu.
/// Onboarding wizard'ın 7 temel adımı tamamlandıktan sonra V5 genişletme adımları kontrol edilir.
/// </summary>
public record GetV5ReadinessCheckQuery(Guid TenantId) : IRequest<V5ReadinessCheckDto>;

public record V5ReadinessCheckDto
{
    public Guid TenantId { get; init; }
    public bool BasicOnboardingCompleted { get; init; }
    public IReadOnlyList<V5FeatureCheckDto> Features { get; init; } = [];
    public int CompletedCount { get; init; }
    public int TotalCount { get; init; }
    public decimal CompletionPercentage { get; init; }
}

public record V5FeatureCheckDto(
    string FeatureName,
    string Description,
    bool IsCompleted,
    string? Details = null);
