using MediatR;
using MesTech.Application.DTOs.Settings;

namespace MesTech.Application.Features.Settings.Queries.GetProfileSettings;

public record GetProfileSettingsQuery(Guid TenantId) : IRequest<ProfileSettingsDto?>;
