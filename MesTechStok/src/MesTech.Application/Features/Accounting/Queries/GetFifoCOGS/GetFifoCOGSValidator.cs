using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;

public sealed class GetFifoCOGSValidator : AbstractValidator<GetFifoCOGSQuery>
{
    public GetFifoCOGSValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
