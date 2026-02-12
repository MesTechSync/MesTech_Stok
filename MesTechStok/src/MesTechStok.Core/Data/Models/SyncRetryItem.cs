using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Bulk sync işlemlerinde hata alan item'ların retry listesi.
    /// Başarısız item'lar burada tutulup daha sonra tekrar denenebilir.
    /// </summary>
    public class SyncRetryItem
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SyncType { get; set; } = string.Empty; // ProductSync, OrderSync, StockSync

        [Required]
        [MaxLength(100)]
        public string ItemId { get; set; } = string.Empty; // Product.Id, Order.Id, etc.

        [Required]
        [MaxLength(50)]
        public string ItemType { get; set; } = string.Empty; // Product, Order, StockLevel

        [MaxLength(1000)]
        public string? ItemData { get; set; } // JSON serialized item data

        [Required]
        [MaxLength(500)]
        public string LastError { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ErrorCategory { get; set; } = string.Empty; // Network, Validation, Auth, Server

        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastRetryUtc { get; set; } = DateTime.UtcNow;
        public DateTime? NextRetryUtc { get; set; }

        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedUtc { get; set; }

        [MaxLength(64)]
        public string? CorrelationId { get; set; }

        [MaxLength(200)]
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Bir sonraki retry zamanını hesaplar (exponential backoff)
        /// </summary>
        public void CalculateNextRetry()
        {
            if (RetryCount >= MaxRetries)
            {
                NextRetryUtc = null;
                return;
            }

            var backoffMinutes = Math.Pow(2, RetryCount) * 5; // 5, 10, 20, 40 dakika
            NextRetryUtc = DateTime.UtcNow.AddMinutes(backoffMinutes);
        }

        /// <summary>
        /// Retry sayısını artırır ve sonraki retry zamanını hesaplar
        /// </summary>
        public void IncrementRetry(string errorMessage, string errorCategory)
        {
            RetryCount++;
            LastRetryUtc = DateTime.UtcNow;
            LastError = errorMessage;
            ErrorCategory = errorCategory;
            CalculateNextRetry();
        }

        /// <summary>
        /// Item'ı çözümlendi olarak işaretler
        /// </summary>
        public void MarkAsResolved()
        {
            IsResolved = true;
            ResolvedUtc = DateTime.UtcNow;
            NextRetryUtc = null;
        }
    }
}
