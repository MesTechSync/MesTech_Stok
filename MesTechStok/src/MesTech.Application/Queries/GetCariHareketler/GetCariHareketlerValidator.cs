using FluentValidation;

namespace MesTech.Application.Queries.GetCariHareketler;

public sealed class GetCariHareketlerValidator : AbstractValidator<GetCariHareketlerQuery>
{
    public GetCariHareketlerValidator()
    {
        RuleFor(x => x.CariHesapId).NotEqual(Guid.Empty).WithMessage("Geçerli cari hesap ID gerekli.");
    }
}
