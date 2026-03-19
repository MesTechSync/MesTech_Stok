using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Crm.Commands.CreateLead;

public record CreateLeadCommand(
    Guid TenantId,
    string FullName,
    LeadSource Source,
    string? Email = null,
    string? Phone = null,
    string? Company = null,
    Guid? StoreId = null,
    Guid? AssignedToUserId = null
) : IRequest<Guid>;
