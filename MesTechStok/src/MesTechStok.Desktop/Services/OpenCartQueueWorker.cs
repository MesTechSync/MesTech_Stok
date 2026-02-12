using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Integrations.OpenCart;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Services
{
    public interface IOpenCartQueueWorker
    {
        void Start();
        void Stop();
    }

    public class OpenCartQueueWorker : IOpenCartQueueWorker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ResilienceOptions _resilienceOptions;
        private readonly OpenCartSettingsOptions _openCartOptions;
        private Timer? _timer;
        private bool _isRunning;

        public OpenCartQueueWorker(IServiceProvider serviceProvider,
                                   IOptions<ResilienceOptions> resilienceOptions,
                                   IOptions<OpenCartSettingsOptions> openCartOptions)
        {
            _serviceProvider = serviceProvider;
            _resilienceOptions = resilienceOptions.Value;
            _openCartOptions = openCartOptions.Value;
        }

        public void Start()
        {
            if (_timer != null) return;
            if (!_openCartOptions.AutoSyncEnabled)
            {
                GlobalLogger.Instance.LogInfo("OpenCartQueueWorker not started (AutoSyncEnabled=false)", "OpenCartQueueWorker");
                return;
            }

            var minutes = Math.Max(1, _openCartOptions.SyncIntervalMinutes);
            var period = TimeSpan.FromMinutes(minutes);
            _timer = new Timer(async _ => await TickWithJitterAsync(), null, TimeSpan.FromSeconds(5), period);
            GlobalLogger.Instance.LogInfo($"OpenCartQueueWorker started, interval={minutes}m (with jitter)", "OpenCartQueueWorker");
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            GlobalLogger.Instance.LogInfo("OpenCartQueueWorker stopped", "OpenCartQueueWorker");
        }

        private async Task TickAsync()
        {
            if (_isRunning) return;
            _isRunning = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IOfflineQueueService>();
                var client = scope.ServiceProvider.GetService<IOpenCartClient>();
                var health = scope.ServiceProvider.GetService<IOpenCartHealthService>();

                var next = await queue.DequeueNextPendingAsync();
                if (next == null) return;

                // Basit kanal yöneticisi: şu an sadece Stock/ProductUpdate Out destekleyelim
                switch ((next.Channel, next.Direction))
                {
                    case ("Stock", "Out"):
                        await ProcessStockUpdateAsync(queue, client, next, health);
                        break;
                    case ("Product", "Out"):
                        await ProcessProductUpdateAsync(queue, client, next, health);
                        break;
                    default:
                        await queue.MarkFailedAsync(next.Id, $"Unsupported channel/direction: {next.Channel}/{next.Direction}");
                        break;
                }
            }
            catch (Exception ex)
            {
                // A++++ LOG İYİLEŞTİRMESİ: Detaylı hata analizi
                var errorDetails = $"Queue worker tick error: {ex.Message}";
                if (ex.InnerException != null)
                    errorDetails += $" Inner: {ex.InnerException.Message}";

                // Kritik hata türlerini tespit et
                if (ex.Message.Contains("Invalid object name"))
                {
                    errorDetails += " [VERİTABANI_TABLO_EKSİK]";
                    GlobalLogger.Instance.LogError($"🚨 KRİTİK: {errorDetails}", "OpenCartQueueWorker");
                }
                else if (ex.Message.Contains("Login failed"))
                {
                    errorDetails += " [VERİTABANI_BAĞLANTI_HATASI]";
                    GlobalLogger.Instance.LogError($"🔌 BAĞLANTI: {errorDetails}", "OpenCartQueueWorker");
                }
                else
                {
                    GlobalLogger.Instance.LogError($"❌ GENEL: {errorDetails}", "OpenCartQueueWorker");
                }
            }
            finally
            {
                _isRunning = false;
            }
        }

        private async Task TickWithJitterAsync()
        {
            // küçük jitter: ±10%
            var baseDelayMs = 0;
            var jitterRatio = 0.10;
            var rnd = new Random();
            var jitter = (int)(baseDelayMs * (rnd.NextDouble() * 2 * jitterRatio - jitterRatio));
            if (jitter > 0)
            {
                await Task.Delay(jitter);
            }
            await TickAsync();
        }

        private static async Task ProcessStockUpdateAsync(IOfflineQueueService queue, IOpenCartClient? client, OfflineQueueItem item, IOpenCartHealthService? health)
        {
            try
            {
                if (client == null)
                {
                    await queue.MarkFailedAsync(item.Id, "OpenCart client not available");
                    health?.OnFailure("Client null");
                    return;
                }

                var doc = JsonSerializer.Deserialize<StockUpdatePayload>(item.Payload ?? "{}");
                if (doc == null || doc.ProductId <= 0)
                {
                    await queue.MarkFailedAsync(item.Id, "Invalid payload for stock update");
                    health?.OnFailure("Invalid stock payload");
                    return;
                }

                var ok = await client.UpdateProductStockAsync(doc.ProductId, doc.Quantity);
                if (ok)
                {
                    await queue.MarkSucceededAsync(item.Id);
                    health?.OnSuccess();
                }
                else
                {
                    await queue.MarkFailedAsync(item.Id, "API stock update failed");
                    health?.OnFailure("API stock update failed");
                }
            }
            catch (Exception ex)
            {
                await queue.MarkFailedAsync(item.Id, ex.Message);
                health?.OnFailure(ex.Message);
            }
        }

        private static async Task ProcessProductUpdateAsync(IOfflineQueueService queue, IOpenCartClient? client, OfflineQueueItem item, IOpenCartHealthService? health)
        {
            try
            {
                if (client == null)
                {
                    await queue.MarkFailedAsync(item.Id, "OpenCart client not available");
                    health?.OnFailure("Client null");
                    return;
                }

                var doc = JsonSerializer.Deserialize<ProductUpdatePayload>(item.Payload ?? "{}");
                if (doc == null || doc.ProductId <= 0)
                {
                    await queue.MarkFailedAsync(item.Id, "Invalid payload for product update");
                    health?.OnFailure("Invalid product payload");
                    return;
                }

                // Şimdilik fiyat güncellemesini örnekleyelim
                var ok = await client.UpdateProductPriceAsync(doc.ProductId, doc.Price);
                if (ok)
                {
                    await queue.MarkSucceededAsync(item.Id);
                    health?.OnSuccess();
                }
                else
                {
                    await queue.MarkFailedAsync(item.Id, "API price update failed");
                    health?.OnFailure("API price update failed");
                }
            }
            catch (Exception ex)
            {
                await queue.MarkFailedAsync(item.Id, ex.Message);
                health?.OnFailure(ex.Message);
            }
        }

        private class StockUpdatePayload
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        private class ProductUpdatePayload
        {
            public int ProductId { get; set; }
            public decimal Price { get; set; }
        }
    }
}


