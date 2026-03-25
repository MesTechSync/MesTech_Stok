using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;

public sealed class CreatePenaltyRecordValidator : AbstractValidator<CreatePenaltyRecordCommand>
{
    public CreatePenaltyRecordValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Description is required and must be at most 500 characters.");
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive.");
        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(3)
            .WithMessage("Currency code must be at most 3 characters.");
        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100)
            .When(x => x.ReferenceNumber != null);
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes != null);
        RuleFor(x => x)
            .Must(x => !x.DueDate.HasValue || x.DueDate.Value >= x.PenaltyDate)
            .WithMessage("Due date must be on or after penalty date.");
    }
}
