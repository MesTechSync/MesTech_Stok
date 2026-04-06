namespace MesTech.Application.Features.Crm.Commands.DeleteCampaign;

public sealed record DeleteCampaignResult(bool IsSuccess, string? ErrorMessage = null);
