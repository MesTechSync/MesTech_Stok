using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;

public sealed class DeletePenaltyRecordValidator : AbstractValidator<DeletePenaltyRecordCommand>
{
    public DeletePenaltyRecordValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
