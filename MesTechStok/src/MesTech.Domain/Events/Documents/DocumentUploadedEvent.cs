using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Documents;
public record DocumentUploadedEvent(Guid DocumentId, string FileName, Guid TenantId, DateTime OccurredAt) : IDomainEvent;
