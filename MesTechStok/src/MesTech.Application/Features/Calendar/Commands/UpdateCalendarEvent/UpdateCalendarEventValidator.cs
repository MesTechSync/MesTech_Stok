using FluentValidation;

namespace MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;

public sealed class UpdateCalendarEventValidator : AbstractValidator<UpdateCalendarEventCommand>
{
    public UpdateCalendarEventValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
