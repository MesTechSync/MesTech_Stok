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

        // K1d-06: Güvenilirlik skorunu ve renk sınıflandırmasını entity üzerinde güncelle
        poolProduct.UpdateReliability(req.NewScore, (int)req.NewColor);

        await poolRepo.UpdatePoolProductAsync(poolProduct, cancellationToken);
        return true;
    }
}
