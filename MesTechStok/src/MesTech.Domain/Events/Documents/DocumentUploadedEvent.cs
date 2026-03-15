using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Documents;
public record DocumentUploadedEvent(Guid DocumentId, string FileName, long FileSizeBytes, Guid TenantId, DateTime OccurredAt) : IDomainEvent;
