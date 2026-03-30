using FluentValidation;

namespace MesTech.Application.Features.Finance.Queries.GetCashRegisters;

public sealed class GetCashRegistersValidator : AbstractValidator<GetCashRegistersQuery>
{
    public GetCashRegistersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
