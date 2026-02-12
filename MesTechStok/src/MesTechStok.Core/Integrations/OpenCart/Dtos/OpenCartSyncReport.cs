using System;
using System.Collections.Generic;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class OpenCartSyncReport
    {
        public DateTime ReportDate { get; set; } = DateTime.Now;
        public TimeSpan TotalDuration { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int SyncedProducts { get; set; }
        public int SyncedOrders { get; set; }
        public int FailedProducts { get; set; }
        public int FailedOrders { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsSuccess => FailedProducts == 0 && FailedOrders == 0;

        public string GetSummary()
        {
            return $"Ürünler: {SyncedProducts}/{TotalProducts}, Siparişler: {SyncedOrders}/{TotalOrders}";
        }
    }
}
