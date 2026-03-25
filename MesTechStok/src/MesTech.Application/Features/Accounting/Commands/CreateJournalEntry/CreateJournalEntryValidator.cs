using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;

public sealed class CreateJournalEntryValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(lines => lines.Count >= 2)
            .WithMessage("A journal entry must have at least 2 lines.");

        RuleFor(x => x.Lines)
            .Must(lines =>
            {
                var totalDebit = lines.Sum(l => l.Debit);
                var totalCredit = lines.Sum(l => l.Credit);
                return totalDebit == totalCredit;
            })
            .WithMessage("Total debit must equal total credit.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.Debit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Credit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l)
                .Must(l => l.Debit > 0 || l.Credit > 0)
                .WithMessage("Either debit or credit must be greater than zero.");
        });
    }
}
