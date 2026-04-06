namespace MesTech.Application.Features.Crm.Commands.DeleteLead;

public sealed record DeleteLeadResult(bool IsSuccess, string? ErrorMessage = null);
