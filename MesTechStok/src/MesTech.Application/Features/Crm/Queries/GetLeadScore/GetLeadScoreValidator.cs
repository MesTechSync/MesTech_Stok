using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetLeadScore;

public sealed class GetLeadScoreValidator : AbstractValidator<GetLeadScoreQuery>
{
    public GetLeadScoreValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.LeadId)
            .NotEqual(Guid.Empty)
            .WithMessage("Lead kimliği boş olamaz.");
    }
}
