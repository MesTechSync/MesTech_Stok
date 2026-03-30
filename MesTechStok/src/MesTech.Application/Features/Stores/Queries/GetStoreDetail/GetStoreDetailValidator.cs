using FluentValidation;

namespace MesTech.Application.Features.Stores.Queries.GetStoreDetail;

public sealed class GetStoreDetailValidator : AbstractValidator<GetStoreDetailQuery>
{
    public GetStoreDetailValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
