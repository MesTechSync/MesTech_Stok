using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record CreateDropshippingPoolCommand(
    string Name,
    string? Description,
    bool IsPublic,
    PoolPricingStrategy PricingStrategy
) : IRequest<Guid>;

public class CreateDropshippingPoolCommandValidator : AbstractValidator<CreateDropshippingPoolCommand>
{
    public CreateDropshippingPoolCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PricingStrategy).IsInEnum();
    }
}

public class CreateDropshippingPoolCommandHandler(
    IDropshippingPoolRepository poolRepo,
    ICurrentUserService currentUser,
    ITenantProvider tenantProvider
) : IRequestHandler<CreateDropshippingPoolCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateDropshippingPoolCommand req, CancellationToken cancellationToken)
    {
        var pool = new DropshippingPool(
            tenantId: tenantProvider.GetCurrentTenantId(),
            name: req.Name,
            description: req.Description,
            isPublic: req.IsPublic,
            pricingStrategy: req.PricingStrategy
        )
        {
            CreatedBy = currentUser.UserId?.ToString() ?? "system"
        };

        await poolRepo.AddAsync(pool, cancellationToken);
        return pool.Id;
    }
}
