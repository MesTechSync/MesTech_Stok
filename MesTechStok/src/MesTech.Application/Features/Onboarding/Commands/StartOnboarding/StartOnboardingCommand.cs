using MediatR;

namespace MesTech.Application.Features.Onboarding.Commands.StartOnboarding;

public record StartOnboardingCommand(Guid TenantId) : IRequest<Guid>;
