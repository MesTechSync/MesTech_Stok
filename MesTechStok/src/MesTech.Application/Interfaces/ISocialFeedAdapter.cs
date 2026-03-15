using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Sosyal ticaret feed adapter'i — her platform (Google Merchant, Facebook Shop vb.) bunu implement eder.
/// </summary>
public interface ISocialFeedAdapter
{
    SocialFeedPlatform Platform { get; }
    Task<FeedGenerationResult> GenerateFeedAsync(FeedGenerationRequest request, CancellationToken ct = default);
    Task<SocialFeedValidationResult> ValidateFeedAsync(string feedUrl, CancellationToken ct = default);
    Task<FeedStatus> GetFeedStatusAsync(CancellationToken ct = default);
    Task ScheduleRefreshAsync(TimeSpan interval, CancellationToken ct = default);
}

public record FeedGenerationRequest(
    Guid StoreId,
    IReadOnlyList<string>? CategoryFilter,
    string Currency = "TRY",
    string Language = "tr");

public record FeedGenerationResult(
    bool Success,
    string? FeedUrl,
    int ItemCount,
    DateTime GeneratedAt,
    IReadOnlyList<string>? Errors = null);

public record SocialFeedValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public record FeedStatus(
    DateTime? LastGenerated,
    int ItemCount,
    DateTime? NextScheduled,
    bool IsHealthy);
