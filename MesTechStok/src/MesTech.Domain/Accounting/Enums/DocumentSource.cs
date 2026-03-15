namespace MesTech.Domain.Accounting.Enums;

/// <summary>
/// Muhasebe belgesi kaynak kanali.
/// </summary>
public enum DocumentSource
{
    WhatsApp = 0,
    Telegram = 1,
    Email = 2,
    Upload = 3,
    Scanner = 4,
    API = 5
}
