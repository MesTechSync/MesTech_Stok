using FluentValidation;

namespace MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;

public class CreateCalendarEventValidator : AbstractValidator<CreateCalendarEventCommand>
{
    public CreateCalendarEventValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300)
            .WithMessage("Title is required and must be at most 300 characters.");
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x)
            .Must(x => x.IsAllDay || x.EndAt > x.StartAt)
            .WithMessage("End time must be after start time for non-all-day events.");
        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description != null);
        RuleFor(x => x.Location)
            .MaximumLength(500)
            .When(x => x.Location != null);
    }
}
