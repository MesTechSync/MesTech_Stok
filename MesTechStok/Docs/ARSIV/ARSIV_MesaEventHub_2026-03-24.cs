// ARŞİV: MesaEventHub.cs
// Tarih: 2026-03-24
// Neden: Program.cs'de MapHub kaydı yok, hiçbir yerden kullanılmıyor (dead code)
// Kaynak: src/MesTech.Infrastructure/Messaging/Hubs/MesaEventHub.cs
// Karar: V5 P2 Faz C audit sonucu arşivlendi. İhtiyaç halinde geri alınabilir.
// MesaEventMessage record'u da bu Hub'a bağlıydı — başka yerde kullanılmıyor.

using Microsoft.AspNetCore.SignalR;

namespace MesTech.Infrastructure.Messaging.Hubs;

public class MesaEventHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = Context.ConnectionId,
            Timestamp = DateTimeOffset.UtcNow
        }).ConfigureAwait(false);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }
}

public record MesaEventMessage
{
    public string EventType { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty; // "Inbound" | "Outbound"
    public string Status { get; init; } = string.Empty;    // "Success" | "Failed" | "Skipped"
    public DateTimeOffset Timestamp { get; init; }
    public string? Details { get; init; }
    public Guid? CorrelationId { get; init; }
}
