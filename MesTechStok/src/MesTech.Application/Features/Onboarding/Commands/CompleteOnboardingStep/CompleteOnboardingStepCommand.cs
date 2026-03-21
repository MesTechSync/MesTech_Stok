using MediatR;

namespace MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;

public record CompleteOnboardingStepCommand(Guid TenantId) : IRequest<Unit>;
