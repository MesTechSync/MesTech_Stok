using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Factory for resolving <see cref="IFeedParserService"/> by <see cref="FeedFormat"/>.
/// Replaces direct IServiceProvider.GetKeyedService usage (ServiceLocator anti-pattern).
/// Panel-E: DEV-E2 (ServiceLocator Killer)
/// </summary>
public interface IFeedParserFactory
{
    /// <summary>
    /// Resolves the <see cref="IFeedParserService"/> for the given <paramref name="format"/>.
    /// Returns <c>null</c> if no parser is registered for the format.
    /// </summary>
    IFeedParserService? GetParser(FeedFormat format);
}
