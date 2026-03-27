using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Dropshipping.Commands;

public sealed class CreateFeedSourceHandler : IRequestHandler<CreateFeedSourceCommand, Guid>
{
    private readonly ISupplierFeedRepository _feedRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateFeedSourceHandler> _logger;

    public CreateFeedSourceHandler(ISupplierFeedRepository feedRepo, IUnitOfWork uow, ILogger<CreateFeedSourceHandler> logger)
    {
        _feedRepo = feedRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateFeedSourceCommand request, CancellationToken cancellationToken)
    {
        var feed = new SupplierFeed
        {
            SupplierId = request.SupplierId,
            Name = request.Name,
            FeedUrl = request.FeedUrl,
            Format = request.Format,
            PriceMarkupPercent = request.PriceMarkupPercent
        };

        await _feedRepo.AddAsync(feed, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Feed source created: {FeedId}, Name={Name}", feed.Id, request.Name);
        return feed.Id;
    }
}
