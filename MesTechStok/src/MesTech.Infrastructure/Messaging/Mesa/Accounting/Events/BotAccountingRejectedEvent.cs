namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// WhatsApp/Panel uzerinden muhasebe belgesi reddedildiginde consume edilir.
/// Exchange: mestech.mesa.bot.accounting.rejected.v1
/// </summary>
public record BotAccountingRejectedEvent(
    Guid DocumentId,
    string RejectedBy,
    string RejectionSource,
    string? RejectionReason,
    Guid TenantId,
    DateTime OccurredAt)
{
    /// <summary>Geriye uyumluluk icin eski Reason property'si.</summary>
    public string Reason => RejectionReason ?? string.Empty;
};
