using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetAppHubData;

public sealed class GetAppHubDataValidator : AbstractValidator<GetAppHubDataQuery>
{
    public GetAppHubDataValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
