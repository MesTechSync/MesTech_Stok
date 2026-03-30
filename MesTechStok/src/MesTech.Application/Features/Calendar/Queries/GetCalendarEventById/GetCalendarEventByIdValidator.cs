using FluentValidation;

namespace MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;

public sealed class GetCalendarEventByIdValidator : AbstractValidator<GetCalendarEventByIdQuery>
{
    public GetCalendarEventByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .WithMessage("Etkinlik kimliği boş olamaz.");
    }
}
