using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetPlatformMessages;

public sealed class GetPlatformMessagesValidator : AbstractValidator<GetPlatformMessagesQuery>
{
    public GetPlatformMessagesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
