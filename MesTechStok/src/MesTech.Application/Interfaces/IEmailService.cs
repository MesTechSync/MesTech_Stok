namespace MesTech.Application.Interfaces;

/// <summary>
/// HTML ve sablon tabanli e-posta gonderim servisi.
/// MailKit SMTP uzerinden calisan genel amacli gonderim.
/// S05 — DEV 6.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// HTML govdeli e-posta gonderir.
    /// </summary>
    Task SendHtmlAsync(string to, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>
    /// NotificationTemplate adindan sablon yukler, {{placeholder}} degistirir ve gonderir.
    /// </summary>
    Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> placeholders, CancellationToken ct = default);
}
