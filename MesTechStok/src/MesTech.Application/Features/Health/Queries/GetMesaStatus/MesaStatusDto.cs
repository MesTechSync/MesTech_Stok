namespace MesTech.Application.Features.Health.Queries.GetMesaStatus;

public sealed class MesaStatusDto
{
    public bool IsConnected { get; init; }
    public DateTime? LastHeartbeat { get; init; }
    public string? Version { get; init; }
    public int ActiveConsumers { get; init; }
    public string BridgeUrl { get; init; } = string.Empty;
    public Dictionary<string, bool> FeatureFlags { get; init; } = new();
    public long? ResponseTimeMs { get; init; }
    public string? ErrorMessage { get; init; }
}
