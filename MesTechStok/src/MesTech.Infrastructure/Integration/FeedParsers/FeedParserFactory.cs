using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.Integration.FeedParsers;

/// <summary>
/// Resolves keyed <see cref="IFeedParserService"/> instances by <see cref="FeedFormat"/>.
/// Encapsulates the keyed-service lookup so consumers use constructor DI instead of
/// raw IServiceProvider (ServiceLocator anti-pattern).
/// Panel-E: DEV-E2 (ServiceLocator Killer)
/// </summary>
public sealed class FeedParserFactory(IServiceProvider serviceProvider) : IFeedParserFactory
{
    public IFeedParserService? GetParser(FeedFormat format)
        => serviceProvider.GetKeyedService<IFeedParserService>(format);
}
