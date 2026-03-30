using FluentValidation;

namespace MesTech.Application.Queries.GetKarZarar;

public sealed class GetKarZararValidator : AbstractValidator<GetKarZararQuery>
{
    public GetKarZararValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
