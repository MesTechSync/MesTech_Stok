using FluentValidation;

namespace MesTech.Application.Features.Hr.Queries.GetTimeEntries;

public sealed class GetTimeEntriesValidator : AbstractValidator<GetTimeEntriesQuery>
{
    public GetTimeEntriesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.To).GreaterThan(x => x.From)
            .WithMessage("Bitis tarihi baslangic tarihinden buyuk olmalidir.");
    }
}
