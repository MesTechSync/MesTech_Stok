using FluentValidation;

namespace MesTech.Application.Features.Stores.Queries.GetStoreCredential;

public sealed class GetStoreCredentialValidator : AbstractValidator<GetStoreCredentialQuery>
{
    public GetStoreCredentialValidator()
    {
        RuleFor(x => x.StoreId).NotEqual(Guid.Empty)
            .WithMessage("Magaza kimlik bilgisi bos olamaz.");
    }
}
