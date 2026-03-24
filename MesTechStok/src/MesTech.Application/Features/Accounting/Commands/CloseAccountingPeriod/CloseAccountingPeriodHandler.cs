using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;

public class CloseAccountingPeriodHandler : IRequestHandler<CloseAccountingPeriodCommand, Guid>
{
    private readonly IAccountingPeriodRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CloseAccountingPeriodHandler> _logger;

    public CloseAccountingPeriodHandler(
        IAccountingPeriodRepository repo,
        IUnitOfWork unitOfWork,
        ILogger<CloseAccountingPeriodHandler> logger)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CloseAccountingPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _repo.GetByYearMonthAsync(
            request.TenantId, request.Year, request.Month, cancellationToken).ConfigureAwait(false);

        if (period is null)
        {
            period = AccountingPeriod.Create(request.TenantId, request.Year, request.Month);
            await _repo.AddAsync(period, cancellationToken).ConfigureAwait(false);
        }

        period.Close(request.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Muhasebe donemi kapatildi — {Year}/{Month}, TenantId={TenantId}, User={UserId}",
            request.Year, request.Month, request.TenantId, request.UserId);

        return period.Id;
    }
}
