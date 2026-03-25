using MesTech.Domain.Events.Documents;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Doküman yüklendiğinde loglama yapar.
/// Gelecekte: OCR/AI işleme tetiklenecek.
/// </summary>
public interface IDocumentUploadedEventHandler
{
    Task HandleAsync(DocumentUploadedEvent domainEvent, CancellationToken ct);
}

public sealed class DocumentUploadedEventHandler : IDocumentUploadedEventHandler
{
    private readonly ILogger<DocumentUploadedEventHandler> _logger;

    public DocumentUploadedEventHandler(ILogger<DocumentUploadedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(DocumentUploadedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "DocumentUploaded — DocumentId={DocumentId}, FileName={FileName}, FileSizeBytes={FileSizeBytes}, TenantId={TenantId}, OccurredAt={OccurredAt}",
            domainEvent.DocumentId, domainEvent.FileName, domainEvent.FileSizeBytes,
            domainEvent.TenantId, domainEvent.OccurredAt);

        // FUTURE: Trigger OCR/AI processing

        return Task.CompletedTask;
    }
}
