using MediatR;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Onboarding.Commands.StartOnboarding;

public sealed class StartOnboardingHandler : IRequestHandler<StartOnboardingCommand, Guid>
{
    private readonly IOnboardingProgressRepository _repository;
    private readonly IUnitOfWork _uow;

    public StartOnboardingHandler(IOnboardingProgressRepository repo, IUnitOfWork uow)
        => (_repository, _uow) = (repo, uow);

    public async Task<Guid> Handle(StartOnboardingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Zaten baslamissa engelle
        var existing = await _repository.GetByTenantIdAsync(request.TenantId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("Bu tenant icin onboarding zaten baslamis.");

        var progress = OnboardingProgress.Start(request.TenantId);
        await _repository.AddAsync(progress, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return progress.Id;
    }
}
