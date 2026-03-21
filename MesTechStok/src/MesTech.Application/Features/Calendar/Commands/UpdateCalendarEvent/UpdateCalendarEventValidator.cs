using FluentValidation;

namespace MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;

public class UpdateCalendarEventValidator : AbstractValidator<UpdateCalendarEventCommand>
{
    public UpdateCalendarEventValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
