using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesTechStok.Desktop.Services
{
    public interface IOpenCartService
    {
        bool IsConnected { get; }
        Task<bool> ConnectAsync(string apiUrl, string apiKey);
        Task<bool> TestConnectionAsync();
        Task<List<OpenCartProduct>> GetProductsAsync();
        Task<bool> SyncProductsAsync();
        Task<SyncResult> QuickSyncAsync();
    }

    public class OpenCartProduct
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class SyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public DateTime SyncTime { get; set; } = DateTime.Now;
    }
}