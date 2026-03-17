using MediatR;

namespace MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;

/// <summary>
/// Yillik vergi takvimi olusturma komutu.
/// 12xKDV + 3xGelirVergisi + 12xSGK + 12xBaBs + 1xYillik = ~40 etkinlik.
/// </summary>
public record GenerateTaxCalendarCommand(int Year, Guid TenantId) : IRequest<int>;
