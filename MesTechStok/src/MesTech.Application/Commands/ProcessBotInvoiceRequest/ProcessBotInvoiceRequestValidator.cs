using FluentValidation;

namespace MesTech.Application.Commands.ProcessBotInvoiceRequest;

public class ProcessBotInvoiceRequestValidator : AbstractValidator<ProcessBotInvoiceRequestCommand>
{
    public ProcessBotInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RequestChannel).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
