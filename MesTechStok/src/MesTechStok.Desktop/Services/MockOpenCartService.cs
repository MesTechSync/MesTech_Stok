using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesTechStok.Desktop.Services
{
    public class MockOpenCartService : IOpenCartService
    {
        private bool _isConnected = false;
        private string _apiUrl = string.Empty;
        private string _apiKey = string.Empty;

        public bool IsConnected => _isConnected;

        private readonly List<OpenCartProduct> _openCartProducts = new()
        {
            new OpenCartProduct { ProductId = 101, Name = "Samsung Galaxy S23", Model = "SM-S911", Price = 25000, Quantity = 10 },
            new OpenCartProduct { ProductId = 102, Name = "iPhone 15 Pro", Model = "A3108", Price = 45000, Quantity = 5 },
            new OpenCartProduct { ProductId = 103, Name = "Sony WH-1000XM5", Model = "WH1000XM5", Price = 8500, Quantity = 20 }
        };

        public Task<bool> ConnectAsync(string apiUrl, string apiKey)
        {
            _apiUrl = apiUrl;
            _apiKey = apiKey;

            // Basit validasyon simülasyonu
            if (!string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey))
            {
                _isConnected = true;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiUrl) || string.IsNullOrWhiteSpace(_apiKey))
                return false;

            // Bağlantı testini simüle et
            await Task.Delay(1500); // API çağrısını simüle et

            _isConnected = true;
            return true;
        }

        public Task<List<OpenCartProduct>> GetProductsAsync()
        {
            if (!_isConnected)
                return Task.FromResult(new List<OpenCartProduct>());

            return Task.FromResult(_openCartProducts);
        }

        public async Task<bool> SyncProductsAsync()
        {
            if (!_isConnected) return false;

            // Senkronizasyon işlemini simüle et
            await Task.Delay(2000);

            return true;
        }

        public async Task<SyncResult> QuickSyncAsync()
        {
            if (!_isConnected)
            {
                return new SyncResult
                {
                    Success = false,
                    Message = "OpenCart'a bağlı değil",
                    ProcessedCount = 0
                };
            }

            // Hızlı senkronizasyonu simüle et
            await Task.Delay(1000);

            return new SyncResult
            {
                Success = true,
                Message = "Hızlı senkronizasyon tamamlandı",
                ProcessedCount = _openCartProducts.Count,
                SyncTime = DateTime.Now
            };
        }
    }
}