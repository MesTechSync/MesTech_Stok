using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.SocialFeed.Commands.RefreshSocialFeed;

public sealed class RefreshSocialFeedHandler
    : IRequestHandler<RefreshSocialFeedCommand, RefreshSocialFeedResult>
{
    private readonly ISocialFeedConfigurationRepository _configRepo;
    private readonly IEnumerable<ISocialFeedAdapter> _adapters;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshSocialFeedHandler> _logger;

    public RefreshSocialFeedHandler(
        ISocialFeedConfigurationRepository configRepo,
        IEnumerable<ISocialFeedAdapter> adapters,
        IUnitOfWork unitOfWork,
        ILogger<RefreshSocialFeedHandler> logger)
    {
        _configRepo = configRepo ?? throw new ArgumentNullException(nameof(configRepo));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RefreshSocialFeedResult> Handle(
        RefreshSocialFeedCommand request, CancellationToken cancellationToken)
    {
        var config = await _configRepo.GetByIdAsync(request.ConfigId, cancellationToken);
        if (config is null)
            return new RefreshSocialFeedResult { IsSuccess = false, ErrorMessage = $"Config {request.ConfigId} not found." };

        var adapterMap = _adapters.ToDictionary(a => a.Platform);
        if (!adapterMap.TryGetValue(config.Platform, out var adapter))
        {
            var msg = $"No adapter for platform {config.Platform}.";
            config.RecordError(msg);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new RefreshSocialFeedResult { IsSuccess = false, ErrorMessage = msg };
        }

        try
        {
            var categoryFilter = string.IsNullOrWhiteSpace(config.CategoryFilter)
                ? null
                : config.CategoryFilter
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

            var feedRequest = new FeedGenerationRequest(
                StoreId: config.TenantId,
                CategoryFilter: categoryFilter?.AsReadOnly(),
                Currency: "TRY",
                Language: "tr");

            var result = await adapter.GenerateFeedAsync(feedRequest, cancellationToken);

            if (result.Success)
            {
                var error = result.Errors is { Count: > 0 } ? string.Join("; ", result.Errors) : null;
                config.RecordGeneration(result.FeedUrl ?? string.Empty, result.ItemCount, error);

                _logger.LogInformation(
                    "SocialFeed generated — Config={ConfigId}, Items={Count}, Url={Url}",
                    config.Id, result.ItemCount, result.FeedUrl);

                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return new RefreshSocialFeedResult
                {
                    IsSuccess = true,
                    ItemCount = result.ItemCount,
                    FeedUrl = result.FeedUrl
                };
            }

            var errorMsg = result.Errors is { Count: > 0 }
                ? string.Join("; ", result.Errors)
                : "Feed generation failed";
            config.RecordError(errorMsg);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new RefreshSocialFeedResult { IsSuccess = false, ErrorMessage = errorMsg };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SocialFeed refresh failed — Config={ConfigId}", config.Id);
            config.RecordError(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new RefreshSocialFeedResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}
