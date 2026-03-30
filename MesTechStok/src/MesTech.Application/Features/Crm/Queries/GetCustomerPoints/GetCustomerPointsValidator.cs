using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetCustomerPoints;

public sealed class GetCustomerPointsValidator : AbstractValidator<GetCustomerPointsQuery>
{
    public GetCustomerPointsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CustomerId)
            .NotEqual(Guid.Empty)
            .WithMessage("Müşteri kimliği boş olamaz.");
    }
}
