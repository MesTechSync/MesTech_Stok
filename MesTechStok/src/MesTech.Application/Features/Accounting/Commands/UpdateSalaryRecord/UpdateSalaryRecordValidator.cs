using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;

public class UpdateSalaryRecordValidator : AbstractValidator<UpdateSalaryRecordCommand>
{
    public UpdateSalaryRecordValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PaymentStatus).IsInEnum();
    }
}
