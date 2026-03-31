using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;

public sealed class CompleteOnboardingStepHandler : IRequestHandler<CompleteOnboardingStepCommand, Unit>
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
        await _repository.UpdateAsync(progress, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
