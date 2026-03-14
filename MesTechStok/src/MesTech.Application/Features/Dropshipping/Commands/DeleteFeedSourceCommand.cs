using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record DeleteFeedSourceCommand(Guid Id) : IRequest<bool>;

public class DeleteFeedSourceCommandHandler(
    ISupplierFeedRepository feedRepo,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteFeedSourceCommand, bool>
{
    public async Task<bool> Handle(
        DeleteFeedSourceCommand req, CancellationToken ct)
    {
        var feed = await feedRepo.GetByIdAsync(req.Id, ct)
            ?? throw new KeyNotFoundException($"SupplierFeed '{req.Id}' bulunamadı.");

        feed.IsDeleted = true;
        feed.DeletedAt = DateTime.UtcNow;
        feed.DeletedBy = currentUser.UserId?.ToString() ?? "system";
        feed.IsActive = false;
        feed.UpdatedAt = DateTime.UtcNow;
        feed.UpdatedBy = currentUser.UserId?.ToString() ?? "system";

        await feedRepo.UpdateAsync(feed, ct);
        return true;
    }
}
