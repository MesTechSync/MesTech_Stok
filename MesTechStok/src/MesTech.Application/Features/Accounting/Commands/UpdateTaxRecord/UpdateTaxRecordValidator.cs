using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;

public class UpdateTaxRecordValidator : AbstractValidator<UpdateTaxRecordCommand>
{
    public UpdateTaxRecordValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
