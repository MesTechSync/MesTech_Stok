using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record RemoveProductFromPoolCommand(Guid PoolProductId) : IRequest<bool>;

public sealed class RemoveProductFromPoolCommandValidator : AbstractValidator<RemoveProductFromPoolCommand>
{
    public RemoveProductFromPoolCommandValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
    }
}

public sealed class RemoveProductFromPoolCommandHandler(
    IDropshippingPoolRepository poolRepo,
    ICurrentUserService currentUser
) : IRequestHandler<RemoveProductFromPoolCommand, bool>
{
    public async Task<bool> Handle(
        RemoveProductFromPoolCommand req, CancellationToken cancellationToken)
    {
        var poolProduct = await poolRepo.GetPoolProductByIdAsync(req.PoolProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"DropshippingPoolProduct '{req.PoolProductId}' bulunamadı.");

        poolProduct.Deactivate();
        poolProduct.IsDeleted = true;
        poolProduct.DeletedAt = DateTime.UtcNow;
        poolProduct.DeletedBy = currentUser.UserId?.ToString() ?? "system";

        await poolRepo.RemovePoolProductAsync(poolProduct, cancellationToken);
        return true;
    }
}
