using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record CreateFeedSourceCommand(
    Guid SupplierId,
    string Name,
    string FeedUrl,
    FeedFormat Format,
    decimal PriceMarkupPercent,
    decimal PriceMarkupFixed,
    int SyncIntervalMinutes,
    string? TargetPlatforms,
    bool AutoDeactivateOnZeroStock
) : IRequest<Guid>;

public class CreateFeedSourceCommandValidator : AbstractValidator<CreateFeedSourceCommand>
{
    public CreateFeedSourceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FeedUrl).NotEmpty().Must(url =>
            Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Geçerli bir URL giriniz.");
        RuleFor(x => x.Format).IsInEnum();
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0).LessThanOrEqualTo(500);
        RuleFor(x => x.SyncIntervalMinutes).InclusiveBetween(5, 1440);
        RuleFor(x => x.SupplierId).NotEmpty();
    }
}

public class CreateFeedSourceCommandHandler(
    ISupplierFeedRepository feedRepo,
    ICurrentUserService currentUser,
    ITenantProvider tenantProvider
) : IRequestHandler<CreateFeedSourceCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateFeedSourceCommand req, CancellationToken cancellationToken)
    {
        var feed = new SupplierFeed
        {
            TenantId = tenantProvider.GetCurrentTenantId(),
            SupplierId = req.SupplierId,
            Name = req.Name,
            FeedUrl = req.FeedUrl,
            Format = req.Format,
            PriceMarkupPercent = req.PriceMarkupPercent,
            PriceMarkupFixed = req.PriceMarkupFixed,
            UsePercentMarkup = req.PriceMarkupPercent > 0,
            SyncIntervalMinutes = req.SyncIntervalMinutes,
            TargetPlatforms = req.TargetPlatforms,
            AutoDeactivateOnZeroStock = req.AutoDeactivateOnZeroStock,
            IsActive = true,
            CreatedBy = currentUser.UserId?.ToString() ?? "system"
        };

        await feedRepo.AddAsync(feed, cancellationToken);
        return feed.Id;
    }
}
