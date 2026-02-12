using System;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class SyncConfiguration
    {
        public bool AutoSync { get; set; }
        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromHours(1);
        public bool SyncProducts { get; set; } = true;
        public bool SyncOrders { get; set; } = true;
        public bool SyncInventory { get; set; } = true;
        public bool SyncPrices { get; set; } = true;
        public DateTime LastSyncDate { get; set; }
        public string SyncDirection { get; set; } = "Bidirectional"; // Bidirectional, ToOpenCart, FromOpenCart
        public int BatchSize { get; set; } = 100;
        public int MaxRetries { get; set; } = 3;
        public bool NotifyOnError { get; set; } = true;
        public bool NotifyOnSuccess { get; set; } = false;
    }
}
