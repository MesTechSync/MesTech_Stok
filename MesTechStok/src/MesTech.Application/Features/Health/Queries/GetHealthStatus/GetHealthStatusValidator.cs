using FluentValidation;

namespace MesTech.Application.Features.Health.Queries.GetHealthStatus;

public sealed class GetHealthStatusValidator : AbstractValidator<GetHealthStatusQuery>
{
    public GetHealthStatusValidator()
    {
        // Parameterless query — no validation rules needed.
    }
}
