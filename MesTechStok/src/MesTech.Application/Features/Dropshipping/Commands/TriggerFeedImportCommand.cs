using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record TriggerFeedImportCommand(Guid FeedId) : IRequest<string>;

public class TriggerFeedImportCommandHandler(
    ISupplierFeedRepository feedRepo,
    IFeedSyncJobService feedSyncJobService
) : IRequestHandler<TriggerFeedImportCommand, string>
{
    public async Task<string> Handle(
        TriggerFeedImportCommand req, CancellationToken ct)
    {
        var feed = await feedRepo.GetByIdAsync(req.FeedId, ct)
            ?? throw new KeyNotFoundException($"SupplierFeed '{req.FeedId}' bulunamadı.");

        // Background job olarak tetikle — blocking değil
        var jobId = feedSyncJobService.EnqueueFeedSync(req.FeedId);
        return jobId;
    }
}
