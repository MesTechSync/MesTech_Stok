using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MesTech.Infrastructure.Email;

// ── Configuration ──

/// <summary>
/// SMTP baglanti ayarlari — appsettings "Smtp" section'indan okunur.
/// </summary>
public sealed record SmtpSettings
{
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string FromAddress { get; init; } = "";
    public string FromName { get; init; } = "MesTech";
    public bool UseSsl { get; init; } = true;
}

// ── Implementation ──

/// <summary>
/// MailKit SMTP ile HTML e-posta gonderimi.
/// UTF-8 Turkce karakter destegi, Base64 content encoding.
/// NotificationTemplate ile sablon tabanli gonderim.
/// S05 — DEV 6.
/// </summary>
public sealed class MailKitEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MailKitEmailService> _logger;

    // Mustache-style placeholder pattern: {{key}}
    private static readonly Regex PlaceholderRegex = new(
        @"\{\{(\w+)\}\}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public MailKitEmailService(
        IOptions<SmtpSettings> settings,
        ITenantProvider tenantProvider,
        ILogger<MailKitEmailService> logger)
    {
        _settings = settings.Value;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendHtmlAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        // HTML govde — UTF-8 + Base64 transfer encoding
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        // Charset UTF-8 zorunlulugu (Turkce karakter destegi)
        foreach (var part in message.BodyParts)
        {
            if (part is MimeKit.TextPart textPart)
            {
                textPart.ContentTransferEncoding = ContentEncoding.Base64;
            }
        }

        await SendAsync(message, ct);

        _logger.LogInformation(
            "[EmailService] E-posta gonderildi: to={To}, subject={Subject}",
            to, subject);
    }

    /// <inheritdoc />
    public async Task SendTemplateAsync(
        string to,
        string templateName,
        Dictionary<string, string> placeholders,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        _logger.LogDebug(
            "[EmailService] Sablon yukleniyor: {TemplateName}", templateName);

        // NotificationTemplate'ten sablon oku
        // Not: Gercek implementasyonda DbContext uzerinden cekecek.
        // Simdilik placeholder-replaced dogrudan gonderim.
        var subject = ReplacePlaceholders(templateName, placeholders);
        var body = BuildTemplateHtml(templateName, placeholders);

        await SendHtmlAsync(to, subject, body, ct);

        _logger.LogInformation(
            "[EmailService] Sablon e-posta gonderildi: to={To}, template={Template}",
            to, templateName);
    }

    /// <summary>
    /// Placeholder'lari gercek degerlerle degistirir.
    /// {{key}} -> value
    /// </summary>
    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        return PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return placeholders.TryGetValue(key, out var value)
                ? value
                : match.Value; // Bilinmeyen placeholder oldugu gibi kalir
        });
    }

    /// <summary>
    /// Basit HTML sablon olusturur — MesTech branding ile.
    /// </summary>
    private static string BuildTemplateHtml(string templateName, Dictionary<string, string> placeholders)
    {
        var bodyContent = string.Join("<br/>",
            placeholders.Select(kv => $"<strong>{kv.Key}:</strong> {kv.Value}"));

        return $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head><meta charset="utf-8"/></head>
            <body style="font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 20px; background: #f5f5f5;">
              <div style="max-width: 600px; margin: 0 auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
                <div style="background: #1976D2; color: #fff; padding: 20px;">
                  <h1 style="margin: 0; font-size: 20px;">MesTech</h1>
                  <p style="margin: 4px 0 0; font-size: 14px; opacity: 0.9;">{templateName}</p>
                </div>
                <div style="padding: 24px;">
                  {bodyContent}
                </div>
                <div style="background: #f0f0f0; padding: 12px 24px; font-size: 12px; color: #666;">
                  Bu e-posta MesTech platformu tarafindan otomatik gonderilmistir.
                </div>
              </div>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// MailKit SmtpClient ile mesaj gonderir.
    /// </summary>
    private async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username))
        {
            _logger.LogWarning(
                "[EmailService] SMTP yapilandirmasi eksik. E-posta gonderilemiyor.");
            return;
        }

        using var client = new SmtpClient();

        try
        {
            var socketOptions = _settings.UseSsl
                ? MailKit.Security.SecureSocketOptions.StartTls
                : MailKit.Security.SecureSocketOptions.Auto;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            await client.SendAsync(message, ct);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, ct);
            }
        }
    }
}
