using FluentValidation;

namespace MesTech.Application.Features.Finance.Commands.CloseCashRegister;

public class CloseCashRegisterValidator : AbstractValidator<CloseCashRegisterCommand>
{
    public CloseCashRegisterValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId bos olamaz.");
        RuleFor(x => x.CashRegisterId).NotEmpty().WithMessage("CashRegisterId bos olamaz.");
        RuleFor(x => x.ClosingDate).NotEmpty().WithMessage("Kapama tarihi bos olamaz.");
        RuleFor(x => x.ActualCashAmount).GreaterThanOrEqualTo(0).WithMessage("Fiziksel sayim tutari negatif olamaz.");
    }
}
