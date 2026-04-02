using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.CreateReconciliationTask;

public record CreateReconciliationTaskCommand : IRequest
{
    public Guid? SettlementBatchId { get; init; }
    public Guid? BankTransactionId { get; init; }
    public decimal Confidence { get; init; }
    public string? Rationale { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class CreateReconciliationTaskHandler : IRequestHandler<CreateReconciliationTaskCommand>
{
    private readonly IReconciliationMatchRepository _matchRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateReconciliationTaskHandler> _logger;

    public CreateReconciliationTaskHandler(
        IReconciliationMatchRepository matchRepo,
        IUnitOfWork uow,
        ILogger<CreateReconciliationTaskHandler> logger)
    {
        _matchRepo = matchRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task Handle(CreateReconciliationTaskCommand request, CancellationToken cancellationToken)
    {
        var match = ReconciliationMatch.Create(
            request.TenantId,
            DateTime.UtcNow,
            request.Confidence,
            ReconciliationStatus.NeedsReview,
            request.SettlementBatchId,
            request.BankTransactionId);

        await _matchRepo.AddAsync(match, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "ReconciliationTask created: MatchId={MatchId} Settlement={Settlement} BankTx={BankTx} Confidence={Confidence}",
            match.Id, request.SettlementBatchId, request.BankTransactionId, request.Confidence);
    }
}
