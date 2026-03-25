using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record DeleteFeedSourceCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteFeedSourceCommandHandler(
    ISupplierFeedRepository feedRepo,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteFeedSourceCommand, bool>
{
    public async Task<bool> Handle(
        DeleteFeedSourceCommand request, CancellationToken cancellationToken)
    {
        var feed = await feedRepo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"SupplierFeed '{request.Id}' bulunamadı.");

        feed.IsDeleted = true;
        feed.DeletedAt = DateTime.UtcNow;
        feed.DeletedBy = currentUser.UserId?.ToString() ?? "system";
        feed.IsActive = false;
        feed.UpdatedAt = DateTime.UtcNow;
        feed.UpdatedBy = currentUser.UserId?.ToString() ?? "system";

        await feedRepo.UpdateAsync(feed, cancellationToken);
        return true;
    }
}
