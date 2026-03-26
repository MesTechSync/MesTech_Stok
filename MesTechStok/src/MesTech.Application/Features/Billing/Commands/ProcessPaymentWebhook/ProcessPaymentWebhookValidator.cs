using FluentValidation;

namespace MesTech.Application.Features.Billing.Commands.ProcessPaymentWebhook;

public sealed class ProcessPaymentWebhookValidator : AbstractValidator<ProcessPaymentWebhookCommand>
{
    public ProcessPaymentWebhookValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RawBody).NotEmpty();
    }
}
