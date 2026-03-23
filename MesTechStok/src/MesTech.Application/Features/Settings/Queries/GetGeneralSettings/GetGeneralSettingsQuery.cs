using MediatR;
using MesTech.Application.DTOs.Settings;

namespace MesTech.Application.Features.Settings.Queries.GetGeneralSettings;

public record GetGeneralSettingsQuery(Guid TenantId) : IRequest<GeneralSettingsDto?>;
