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
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DocumentCount).GreaterThanOrEqualTo(0);
    }
}
