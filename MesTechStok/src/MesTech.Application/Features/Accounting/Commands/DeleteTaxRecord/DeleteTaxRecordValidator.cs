using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.DeleteTaxRecord;

public class DeleteTaxRecordValidator : AbstractValidator<DeleteTaxRecordCommand>
{
    public DeleteTaxRecordValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
