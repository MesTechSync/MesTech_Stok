namespace MesTechStok.Desktop.Services;

public class ResilienceOptions
{
    public int MaxRetries { get; set; } = 3;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 30;
}

public class OpenCartSettingsOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiUrl { get => BaseUrl; set => BaseUrl = value; }
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool AutoSyncEnabled { get; set; }
    public int SyncIntervalMinutes { get; set; } = 15;
}
