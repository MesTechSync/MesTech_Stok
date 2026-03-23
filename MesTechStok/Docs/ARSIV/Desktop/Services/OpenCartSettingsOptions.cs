namespace MesTechStok.Desktop.Services
{
    public class OpenCartSettingsOptions
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool AutoSyncEnabled { get; set; } = false;
        public int SyncIntervalMinutes { get; set; } = 30;
    }
}


