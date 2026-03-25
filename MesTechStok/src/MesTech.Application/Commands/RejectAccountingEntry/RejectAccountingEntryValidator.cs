using FluentValidation;

namespace MesTech.Application.Commands.RejectAccountingEntry;

public sealed class RejectAccountingEntryValidator : AbstractValidator<RejectAccountingEntryCommand>
{
    public RejectAccountingEntryValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.RejectedBy).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RejectionSource).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
