using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.ApproveAccountingEntry;

public record ApproveAccountingEntryCommand : IRequest
{
    public Guid DocumentId { get; init; }
    public string ApprovedBy { get; init; } = string.Empty;
    public string ApprovalSource { get; init; } = string.Empty;
    public Guid? JournalEntryId { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class ApproveAccountingEntryHandler : IRequestHandler<ApproveAccountingEntryCommand>
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveAccountingEntryHandler> _logger;

    public ApproveAccountingEntryHandler(
        IJournalEntryRepository journalEntryRepository,
        IUnitOfWork unitOfWork,
        ILogger<ApproveAccountingEntryHandler> logger)
    {
        _journalEntryRepository = journalEntryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ApproveAccountingEntryCommand request, CancellationToken cancellationToken)
    {
        if (request.JournalEntryId is null)
        {
            _logger.LogWarning("ApproveAccountingEntry: JournalEntryId is null, skipping");
            return;
        }

        var entry = await _journalEntryRepository.GetByIdAsync(request.JournalEntryId.Value, cancellationToken).ConfigureAwait(false);
        if (entry is null)
        {
            _logger.LogWarning("ApproveAccountingEntry: JournalEntry {Id} not found", request.JournalEntryId);
            return;
        }

        if (entry.IsPosted)
        {
            _logger.LogInformation("ApproveAccountingEntry: JournalEntry {Id} already posted", request.JournalEntryId);
            return;
        }

        entry.Post();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("ApproveAccountingEntry: JournalEntry {Id} posted by {ApprovedBy} via {Source}",
            request.JournalEntryId, request.ApprovedBy, request.ApprovalSource);
    }
}
