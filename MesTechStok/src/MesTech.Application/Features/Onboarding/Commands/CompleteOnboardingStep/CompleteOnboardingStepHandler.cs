using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;

public class CompleteOnboardingStepHandler : IRequestHandler<CompleteOnboardingStepCommand, Unit>
{
    private readonly IOnboardingProgressRepository _repository;
    private readonly IUnitOfWork _uow;

    public CompleteOnboardingStepHandler(IOnboardingProgressRepository repo, IUnitOfWork uow)
        => (_repository, _uow) = (repo, uow);

    public async Task<Unit> Handle(CompleteOnboardingStepCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var progress = await _repository.GetByTenantIdAsync(request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException("Onboarding bulunamadi. Once StartOnboarding cagirin.");

        progress.CompleteCurrentStep();
        await _repository.UpdateAsync(progress, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
