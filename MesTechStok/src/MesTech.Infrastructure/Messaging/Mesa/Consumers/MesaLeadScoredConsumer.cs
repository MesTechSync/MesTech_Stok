using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Consumers;

/// <summary>
/// MESA AI lead'e skor atadığında consume eder.
/// Lead entity'ye skor bilgisi persist edilir ve domain event yayınlanır.
/// </summary>
public sealed class MesaLeadScoredConsumer : IConsumer<MesaLeadScoredEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ICrmLeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MesaLeadScoredConsumer> _logger;

    public MesaLeadScoredConsumer(
        IMesaEventMonitor monitor,
        ICrmLeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        ILogger<MesaLeadScoredConsumer> logger)
    {
        _monitor = monitor;
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaLeadScoredEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Processing {Consumer} MessageId={MessageId} — LeadId={LeadId} Score={Score}",
            nameof(MesaLeadScoredConsumer), context.MessageId, msg.LeadId, msg.Score);

        try
        {
            var lead = await _leadRepository.GetByIdAsync(msg.LeadId).ConfigureAwait(false);
            if (lead is null)
            {
                _logger.LogWarning(
                    "Lead {LeadId} not found for scoring — ignoring event MessageId={MessageId}",
                    msg.LeadId, context.MessageId);
                return;
            }

            lead.UpdateScore(msg.Score, msg.Reasoning);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Lead {LeadId} ({FullName}) scored {Score}/100 — Reasoning: {Reasoning}",
                msg.LeadId, lead.FullName, msg.Score, msg.Reasoning);

            _monitor.RecordConsume(nameof(MesaLeadScoredEvent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consumer {Consumer} failed for MessageId={MessageId}",
                nameof(MesaLeadScoredConsumer), context.MessageId);
            throw; // MassTransit retry'a bırak
        }
    }
}
