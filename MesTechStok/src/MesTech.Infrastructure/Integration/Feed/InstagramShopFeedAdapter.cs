using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Integration.Feed;

/// <summary>
/// Instagram Shop feed adapter.
/// Thin wrapper over <see cref="FacebookShopFeedAdapter"/> — Instagram uses the same
/// Facebook Commerce Manager catalog API. Only the Platform property differs.
/// </summary>
public sealed class InstagramShopFeedAdapter : FacebookShopFeedAdapter
{
    public override SocialFeedPlatform Platform => SocialFeedPlatform.InstagramShop;

    public InstagramShopFeedAdapter(
        AppDbContext dbContext,
        ILogger<InstagramShopFeedAdapter> logger,
        IOptions<FeedOptions>? feedOptions = null)
        : base(dbContext, logger, feedOptions)
    {
    }
}
