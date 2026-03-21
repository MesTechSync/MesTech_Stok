using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;

public class DeleteSalaryRecordValidator : AbstractValidator<DeleteSalaryRecordCommand>
{
    public DeleteSalaryRecordValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
