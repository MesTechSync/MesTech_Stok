namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// WhatsApp/Panel uzerinden muhasebe belgesi onaylandiginda consume edilir.
/// Exchange: mestech.mesa.bot.accounting.approved.v1
/// </summary>
public record BotAccountingApprovedEvent(
    Guid DocumentId,
    string ApprovedBy,
    string ApprovalSource,
    Guid? JournalEntryId,
    Guid TenantId,
    DateTime OccurredAt);
