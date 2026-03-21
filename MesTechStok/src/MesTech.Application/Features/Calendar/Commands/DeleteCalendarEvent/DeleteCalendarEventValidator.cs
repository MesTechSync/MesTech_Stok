using FluentValidation;

namespace MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;

public class DeleteCalendarEventValidator : AbstractValidator<DeleteCalendarEventCommand>
{
    public DeleteCalendarEventValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
