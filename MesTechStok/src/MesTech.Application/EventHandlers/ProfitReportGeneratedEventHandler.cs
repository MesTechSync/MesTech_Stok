using MesTech.Domain.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IProfitReportGeneratedEventHandler
{
    Task HandleAsync(ProfitReportGeneratedEvent domainEvent, CancellationToken ct);
}

public class ProfitReportGeneratedEventHandler : IProfitReportGeneratedEventHandler
{
    private readonly ILogger<ProfitReportGeneratedEventHandler> _logger;

    public ProfitReportGeneratedEventHandler(ILogger<ProfitReportGeneratedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(ProfitReportGeneratedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Kâr/zarar raporu oluştu — ReportId={Id}, Period={Period}, Platform={Platform}, NetProfit={Profit}",
            domainEvent.ReportId, domainEvent.Period, domainEvent.Platform, domainEvent.NetProfit);
        return Task.CompletedTask;
    }
}
