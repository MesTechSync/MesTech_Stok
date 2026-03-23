using MediatR;
using MesTech.Application.DTOs.Settings;

namespace MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;

public record GetCredentialsSettingsQuery(Guid TenantId) : IRequest<CredentialsSettingsDto>;
