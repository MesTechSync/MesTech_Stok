using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record UpdatePoolProductStockCommand(
    Guid PoolProductId,
    decimal NewPrice
) : IRequest<bool>;

public sealed class UpdatePoolProductStockCommandValidator : AbstractValidator<UpdatePoolProductStockCommand>
{
    public UpdatePoolProductStockCommandValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
        RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePoolProductStockCommandHandler(
    IDropshippingPoolRepository poolRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdatePoolProductStockCommand, bool>
{
    public async Task<bool> Handle(
        UpdatePoolProductStockCommand req, CancellationToken cancellationToken)
    {
        var poolProduct = await poolRepo.GetPoolProductByIdAsync(req.PoolProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"DropshippingPoolProduct '{req.PoolProductId}' bulunamadı.");

        poolProduct.UpdatePrice(req.NewPrice);
        poolProduct.UpdatedBy = currentUser.UserId?.ToString() ?? "system";

        await poolRepo.UpdatePoolProductAsync(poolProduct, cancellationToken);
        return true;
    }
}
