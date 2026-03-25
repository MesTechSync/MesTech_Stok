using FluentValidation;

namespace MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;

public sealed class GenerateTaxCalendarValidator : AbstractValidator<GenerateTaxCalendarCommand>
{
    public GenerateTaxCalendarValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
