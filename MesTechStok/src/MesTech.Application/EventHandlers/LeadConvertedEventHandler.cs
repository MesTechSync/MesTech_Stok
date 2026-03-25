using MesTech.Domain.Events.Crm;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface ILeadConvertedEventHandler
{
    Task HandleAsync(LeadConvertedEvent domainEvent, CancellationToken ct);
}

public sealed class LeadConvertedEventHandler : ILeadConvertedEventHandler
{
    private readonly ILogger<LeadConvertedEventHandler> _logger;

    public LeadConvertedEventHandler(ILogger<LeadConvertedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(LeadConvertedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "LeadConverted: LeadId={LeadId}, ContactId={ContactId}",
            domainEvent.LeadId, domainEvent.CrmContactId);

        return Task.CompletedTask;
    }
}
