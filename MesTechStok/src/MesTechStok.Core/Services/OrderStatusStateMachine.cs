using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services
{
    /// <summary>
    /// OrderStatus durum geçişleri için domain event modeli
    /// </summary>
    public class OrderStatusChangedEvent
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public OrderStatus PreviousStatus { get; set; }
        public OrderStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? ChangedBy { get; set; }
        public string? Reason { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// OrderStatus geçiş doğrulama sonucu
    /// </summary>
    public class StatusTransitionResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
        public bool RequiresConfirmation { get; set; }

        public static StatusTransitionResult Success() => new() { IsValid = true };
        public static StatusTransitionResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
        public static StatusTransitionResult Warning(string message) => new() { IsValid = true, Warnings = { message } };
    }

    /// <summary>
    /// OrderStatus State Machine interface
    /// Sipariş durumları arasındaki geçişleri yönetir ve doğrular
    /// </summary>
    public interface IOrderStatusStateMachine
    {
        Task<StatusTransitionResult> ValidateTransitionAsync(OrderStatus from, OrderStatus to, Order order);
        Task<StatusTransitionResult> TransitionToAsync(Order order, OrderStatus newStatus, string? reason = null, string? changedBy = null);
        IEnumerable<OrderStatus> GetValidNextStates(OrderStatus currentStatus);
        bool IsValidTransition(OrderStatus from, OrderStatus to);
        event EventHandler<OrderStatusChangedEvent>? StatusChanged;
    }

    /// <summary>
    /// OrderStatus State Machine implementasyonu
    /// İş kuralları ve geçiş doğrulamaları ile sipariş durumlarını yönetir
    /// </summary>
    public class OrderStatusStateMachine : IOrderStatusStateMachine
    {
        private readonly ILogger<OrderStatusStateMachine> _logger;

        // Geçerli durum geçişleri matrisi
        private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
        {
            [OrderStatus.Pending] = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new() { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new() { OrderStatus.Delivered, OrderStatus.Cancelled },
            [OrderStatus.Delivered] = new() { }, // Son durum - başka duruma geçiş yok
            [OrderStatus.Cancelled] = new() { } // Son durum - başka duruma geçiş yok
        };

        // Onay gerektiren geçişler
        private static readonly HashSet<(OrderStatus From, OrderStatus To)> ConfirmationRequired = new()
        {
            (OrderStatus.Confirmed, OrderStatus.Cancelled),
            (OrderStatus.Shipped, OrderStatus.Cancelled)
        };

        public event EventHandler<OrderStatusChangedEvent>? StatusChanged;

        public OrderStatusStateMachine(ILogger<OrderStatusStateMachine> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Durum geçişinin geçerli olup olmadığını kontrol eder
        /// </summary>
        public bool IsValidTransition(OrderStatus from, OrderStatus to)
        {
            if (from == to) return false; // Aynı duruma geçiş geçersiz

            return ValidTransitions.TryGetValue(from, out var validNextStates)
                   && validNextStates.Contains(to);
        }

        /// <summary>
        /// Mevcut durumdan geçilebilecek geçerli durumları döner
        /// </summary>
        public IEnumerable<OrderStatus> GetValidNextStates(OrderStatus currentStatus)
        {
            return ValidTransitions.TryGetValue(currentStatus, out var validStates)
                ? validStates
                : Enumerable.Empty<OrderStatus>();
        }

        /// <summary>
        /// Durum geçişini doğrular (iş kuralları dahil)
        /// </summary>
        public async Task<StatusTransitionResult> ValidateTransitionAsync(OrderStatus from, OrderStatus to, Order order)
        {
            // Temel geçiş kontrolü
            if (!IsValidTransition(from, to))
            {
                return StatusTransitionResult.Error($"Invalid transition from {from} to {to}");
            }

            // İş kuralı kontrolları
            var businessRuleResult = await ValidateBusinessRulesAsync(from, to, order);
            if (!businessRuleResult.IsValid)
            {
                return businessRuleResult;
            }

            // Onay gerektirip gerektirmediğini kontrol et
            if (ConfirmationRequired.Contains((from, to)))
            {
                businessRuleResult.RequiresConfirmation = true;
                businessRuleResult.Warnings.Add($"Transition from {from} to {to} requires confirmation");
            }

            return businessRuleResult;
        }

        /// <summary>
        /// Sipariş durumunu yeni duruma geçirir
        /// </summary>
        public async Task<StatusTransitionResult> TransitionToAsync(Order order, OrderStatus newStatus, string? reason = null, string? changedBy = null)
        {
            var currentStatus = order.Status;

            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();
            _logger.LogInformation("[StatusTransition] Order {OrderId} {OrderNumber}: {From} → {To}. Reason: {Reason}, ChangedBy: {ChangedBy}, CorrelationId: {CorrelationId}",
                order.Id, order.OrderNumber, currentStatus, newStatus, reason ?? "N/A", changedBy ?? "System",
                MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

            // Doğrulama
            var validationResult = await ValidateTransitionAsync(currentStatus, newStatus, order);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("[StatusTransition] Validation failed for Order {OrderId}: {Error}",
                    order.Id, validationResult.ErrorMessage);
                return validationResult;
            }

            try
            {
                // Durum değişikliğini uygula
                var previousStatus = order.Status;
                order.Status = newStatus;
                order.ModifiedDate = DateTime.UtcNow;

                // Event fırlat
                var statusEvent = new OrderStatusChangedEvent
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    ChangedBy = changedBy,
                    Reason = reason,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["CorrelationId"] = MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId ?? string.Empty,
                        ["TransitionTime"] = DateTime.UtcNow
                    }
                };

                // Side effects'leri uygula
                await ApplySideEffectsAsync(order, previousStatus, newStatus);

                StatusChanged?.Invoke(this, statusEvent);

                _logger.LogInformation("[StatusTransition] Successfully transitioned Order {OrderId} from {From} to {To}",
                    order.Id, previousStatus, newStatus);

                return StatusTransitionResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StatusTransition] Failed to transition Order {OrderId} from {From} to {To}",
                    order.Id, currentStatus, newStatus);

                return StatusTransitionResult.Error($"Transition failed: {ex.Message}");
            }
        }

        /// <summary>
        /// İş kuralları doğrulaması
        /// </summary>
        private async Task<StatusTransitionResult> ValidateBusinessRulesAsync(OrderStatus from, OrderStatus to, Order order)
        {
            var result = StatusTransitionResult.Success();

            // Pending → Confirmed: Stok kontrolü
            if (from == OrderStatus.Pending && to == OrderStatus.Confirmed)
            {
                var hasStockIssues = await CheckStockAvailabilityAsync(order);
                if (hasStockIssues)
                {
                    result.Warnings.Add("Some items may have stock issues");
                }
            }

            // Confirmed → Shipped: Hazırlık kontrolü
            if (from == OrderStatus.Confirmed && to == OrderStatus.Shipped)
            {
                if (order.RequiredDate.HasValue && order.RequiredDate < DateTime.Now.AddDays(-1))
                {
                    result.Warnings.Add("Order is being shipped after required date");
                }
            }

            // Shipped → Delivered: Teslimat doğrulaması
            if (from == OrderStatus.Shipped && to == OrderStatus.Delivered)
            {
                // Teslimat için ek doğrulamalar burada yapılabilir
                result.Warnings.Add("Delivery confirmation may be required");
            }

            // İptal kontrolü
            if (to == OrderStatus.Cancelled)
            {
                if (from == OrderStatus.Delivered)
                {
                    return StatusTransitionResult.Error("Cannot cancel a delivered order");
                }

                if (from == OrderStatus.Shipped)
                {
                    result.Warnings.Add("Cancelling a shipped order may require additional processes");
                }
            }

            return result;
        }

        /// <summary>
        /// Durum değişikliği yan etkileri
        /// </summary>
        private async Task ApplySideEffectsAsync(Order order, OrderStatus from, OrderStatus to)
        {
            // Confirmed: Stok rezervasyonu
            if (to == OrderStatus.Confirmed)
            {
                _logger.LogDebug("[StatusTransition] Applying stock reservation for Order {OrderId}", order.Id);
                // TODO: Stok rezervasyon logic'i
            }

            // Cancelled: Stok rezervasyonu iptal
            if (to == OrderStatus.Cancelled && from == OrderStatus.Confirmed)
            {
                _logger.LogDebug("[StatusTransition] Releasing stock reservation for Order {OrderId}", order.Id);
                // TODO: Stok rezervasyon iptal logic'i
            }

            // Shipped: Kargo entegrasyonu
            if (to == OrderStatus.Shipped)
            {
                _logger.LogDebug("[StatusTransition] Triggering shipping integration for Order {OrderId}", order.Id);
                // TODO: Kargo entegrasyon logic'i
            }

            // Delivered: Fatura oluşturma
            if (to == OrderStatus.Delivered)
            {
                _logger.LogDebug("[StatusTransition] Triggering invoice generation for Order {OrderId}", order.Id);
                // TODO: Fatura oluşturma logic'i
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Stok durumu kontrolü
        /// </summary>
        private async Task<bool> CheckStockAvailabilityAsync(Order order)
        {
            // TODO: Gerçek stok kontrol logic'i
            await Task.Delay(10); // Placeholder
            return false; // Şimdilik stok sorunu yok varsayımı
        }
    }

    /// <summary>
    /// OrderStatus State Machine extension methods
    /// </summary>
    public static class OrderStatusExtensions
    {
        /// <summary>
        /// Sipariş durumunun son durum olup olmadığını kontrol eder
        /// </summary>
        public static bool IsTerminalStatus(this OrderStatus status)
        {
            return status == OrderStatus.Delivered || status == OrderStatus.Cancelled;
        }

        /// <summary>
        /// Sipariş durumunun aktif olup olmadığını kontrol eder
        /// </summary>
        public static bool IsActiveStatus(this OrderStatus status)
        {
            return status != OrderStatus.Cancelled && status != OrderStatus.Delivered;
        }

        /// <summary>
        /// Durum için display name
        /// </summary>
        public static string GetDisplayName(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Beklemede",
                OrderStatus.Confirmed => "Onaylandı",
                OrderStatus.Shipped => "Sevk Edildi",
                OrderStatus.Delivered => "Teslim Edildi",
                OrderStatus.Cancelled => "İptal Edildi",
                _ => status.ToString()
            };
        }

        /// <summary>
        /// Durum için renk kodu (UI için)
        /// </summary>
        public static string GetStatusColor(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "#FF9800",      // Orange
                OrderStatus.Confirmed => "#2196F3",    // Blue  
                OrderStatus.Shipped => "#9C27B0",      // Purple
                OrderStatus.Delivered => "#4CAF50",    // Green
                OrderStatus.Cancelled => "#F44336",    // Red
                _ => "#757575"                          // Gray
            };
        }
    }
}
