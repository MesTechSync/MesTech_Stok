using FluentValidation;

namespace MesTech.Application.Commands.ProcessBotReturnRequest;

public sealed class ProcessBotReturnRequestValidator : AbstractValidator<ProcessBotReturnRequestCommand>
{
    public ProcessBotReturnRequestValidator()
    {
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RequestChannel).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
