using FluentValidation;

namespace MesTech.Application.Features.Finance.Commands.CreateCashRegister;

public sealed class CreateCashRegisterValidator : AbstractValidator<CreateCashRegisterCommand>
{
    public CreateCashRegisterValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}
