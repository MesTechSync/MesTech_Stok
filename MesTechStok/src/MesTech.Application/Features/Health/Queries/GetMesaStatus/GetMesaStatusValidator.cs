using FluentValidation;

namespace MesTech.Application.Features.Health.Queries.GetMesaStatus;

public sealed class GetMesaStatusValidator : AbstractValidator<GetMesaStatusQuery>
{
    public GetMesaStatusValidator()
    {
        // Parameterless query — no validation rules needed.
    }
}
