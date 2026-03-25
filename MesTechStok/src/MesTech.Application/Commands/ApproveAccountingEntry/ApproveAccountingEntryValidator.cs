using FluentValidation;

namespace MesTech.Application.Commands.ApproveAccountingEntry;

public sealed class ApproveAccountingEntryValidator : AbstractValidator<ApproveAccountingEntryCommand>
{
    public ApproveAccountingEntryValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.ApprovedBy).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ApprovalSource).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
