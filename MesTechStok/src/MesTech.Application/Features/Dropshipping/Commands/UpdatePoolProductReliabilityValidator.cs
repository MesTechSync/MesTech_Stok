using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public class UpdatePoolProductReliabilityValidator : AbstractValidator<UpdatePoolProductReliabilityCommand>
{
    public UpdatePoolProductReliabilityValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
    }
}
