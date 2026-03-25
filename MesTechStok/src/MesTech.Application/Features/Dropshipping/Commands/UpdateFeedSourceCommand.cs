using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record UpdateFeedSourceCommand(
    Guid Id,
    string Name,
    string FeedUrl,
    FeedFormat Format,
    decimal PriceMarkupPercent,
    decimal PriceMarkupFixed,
    int SyncIntervalMinutes,
    string? TargetPlatforms,
    bool AutoDeactivateOnZeroStock,
    bool IsActive
) : IRequest<bool>;

public sealed class UpdateFeedSourceCommandValidator : AbstractValidator<UpdateFeedSourceCommand>
{
    public UpdateFeedSourceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FeedUrl).NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Geçerli bir URL giriniz.");
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SyncIntervalMinutes).InclusiveBetween(5, 1440);
    }
}

public sealed class UpdateFeedSourceCommandHandler(
    ISupplierFeedRepository feedRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateFeedSourceCommand, bool>
{
    public async Task<bool> Handle(
        UpdateFeedSourceCommand req, CancellationToken cancellationToken)
    {
        var feed = await feedRepo.GetByIdAsync(req.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"SupplierFeed '{req.Id}' bulunamadı.");

        feed.Name = req.Name;
        feed.FeedUrl = req.FeedUrl;
        feed.Format = req.Format;
        feed.PriceMarkupPercent = req.PriceMarkupPercent;
        feed.PriceMarkupFixed = req.PriceMarkupFixed;
        feed.UsePercentMarkup = req.PriceMarkupPercent > 0;
        feed.SyncIntervalMinutes = req.SyncIntervalMinutes;
        feed.TargetPlatforms = req.TargetPlatforms;
        feed.AutoDeactivateOnZeroStock = req.AutoDeactivateOnZeroStock;
        feed.IsActive = req.IsActive;
        feed.UpdatedAt = DateTime.UtcNow;
        feed.UpdatedBy = currentUser.UserId?.ToString() ?? "system";

        await feedRepo.UpdateAsync(feed, cancellationToken);
        return true;
    }
}
