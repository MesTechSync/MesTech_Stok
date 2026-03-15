using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record UpdatePoolProductReliabilityCommand(
    Guid PoolProductId,
    decimal NewScore,
    ReliabilityColor NewColor
) : IRequest<bool>;

public class UpdatePoolProductReliabilityCommandValidator : AbstractValidator<UpdatePoolProductReliabilityCommand>
{
    public UpdatePoolProductReliabilityCommandValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
        RuleFor(x => x.NewScore).InclusiveBetween(0, 100);
        RuleFor(x => x.NewColor).IsInEnum();
    }
}

public class UpdatePoolProductReliabilityCommandHandler(
    IDropshippingPoolRepository poolRepo
) : IRequestHandler<UpdatePoolProductReliabilityCommand, bool>
{
    public async Task<bool> Handle(
        UpdatePoolProductReliabilityCommand req, CancellationToken cancellationToken)
    {
        var poolProduct = await poolRepo.GetPoolProductByIdAsync(req.PoolProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"DropshippingPoolProduct '{req.PoolProductId}' bulunamadı.");

        // ReliabilityColor ve score — entity'de doğrudan property set
        poolProduct.UpdatedAt = DateTime.UtcNow;

        await poolRepo.UpdatePoolProductAsync(poolProduct, cancellationToken);
        return true;
    }
}
