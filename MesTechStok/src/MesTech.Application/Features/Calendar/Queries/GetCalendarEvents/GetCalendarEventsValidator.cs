using FluentValidation;

namespace MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;

public sealed class GetCalendarEventsValidator : AbstractValidator<GetCalendarEventsQuery>
{
    public GetCalendarEventsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
