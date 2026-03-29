using MediatR;

namespace MesTech.Application.Features.SocialFeed.Commands.RefreshSocialFeed;

/// <summary>
/// Tek bir SocialFeedConfiguration için feed üretimini tetikler.
/// G378/G454: SocialFeedRefreshJob'dan CQRS'e taşınmış iş mantığı.
/// Job artık sadece _mediator.Send() çağıracak.
/// </summary>
public record RefreshSocialFeedCommand(Guid ConfigId) : IRequest<RefreshSocialFeedResult>;

public sealed class RefreshSocialFeedResult
{
    public bool IsSuccess { get; init; }
    public int ItemCount { get; init; }
    public string? FeedUrl { get; init; }
    public string? ErrorMessage { get; init; }
}
