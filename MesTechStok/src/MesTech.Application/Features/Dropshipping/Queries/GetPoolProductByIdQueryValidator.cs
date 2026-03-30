using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries;

public sealed class GetPoolProductByIdQueryValidator : AbstractValidator<GetPoolProductByIdQuery>
{
    public GetPoolProductByIdQueryValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
    }
}
