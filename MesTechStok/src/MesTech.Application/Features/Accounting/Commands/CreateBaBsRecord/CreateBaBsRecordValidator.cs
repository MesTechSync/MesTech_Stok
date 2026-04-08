using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;

public sealed class CreateBaBsRecordValidator : AbstractValidator<CreateBaBsRecordCommand>
{
    public CreateBaBsRecordValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.CounterpartyVkn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CounterpartyName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TotalAmount).GreaterThan(0).WithMessage("BaBs toplam tutar sıfırdan büyük olmalıdır.");
        RuleFor(x => x.DocumentCount).GreaterThan(0).WithMessage("Belge sayısı sıfırdan büyük olmalıdır.");
    }
}
