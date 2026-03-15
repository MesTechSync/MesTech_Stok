namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// MESA AI finansal danismanlik onerisi geldiginde consume edilir.
/// Exchange: mestech.mesa.ai.advisory.recommendation.v1
/// </summary>
public record AiAdvisoryRecommendationEvent(
    string RecommendationType,
    string Title,
    string Description,
    string? ActionUrl,
    string Priority,
    Guid TenantId,
    DateTime OccurredAt);
