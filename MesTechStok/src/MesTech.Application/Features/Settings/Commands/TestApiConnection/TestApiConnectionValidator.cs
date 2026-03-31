using FluentValidation;

namespace MesTech.Application.Features.Settings.Commands.TestApiConnection;

public sealed class TestApiConnectionValidator : AbstractValidator<TestApiConnectionCommand>
{
    public TestApiConnectionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ApiBaseUrl)
            .NotEmpty()
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ApiBaseUrl must be a valid absolute URL.");
    }
}
