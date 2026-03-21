using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;

public class UpdatePenaltyRecordValidator : AbstractValidator<UpdatePenaltyRecordCommand>
{
    public UpdatePenaltyRecordValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PaymentStatus).IsInEnum();
    }
}
