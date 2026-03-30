using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetJournalEntries;

public sealed class GetJournalEntriesValidator : AbstractValidator<GetJournalEntriesQuery>
{
    public GetJournalEntriesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.From).NotEmpty();
        RuleFor(x => x.To).NotEmpty().GreaterThanOrEqualTo(x => x.From);
    }
}
