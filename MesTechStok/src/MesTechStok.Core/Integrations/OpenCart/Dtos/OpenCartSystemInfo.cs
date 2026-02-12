using System;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class OpenCartSystemInfo
    {
        public string Version { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime LastChecked { get; set; } = DateTime.Now;
        public string StoreName { get; set; } = string.Empty;
        public string StoreUrl { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
    }
}
