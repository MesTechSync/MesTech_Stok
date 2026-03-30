using FluentValidation;

namespace MesTech.Application.Features.Settings.Commands.TestErpConnection;

public sealed class TestErpConnectionValidator : AbstractValidator<TestErpConnectionCommand>
{
    public TestErpConnectionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ErpProvider).IsInEnum();
    }
}
