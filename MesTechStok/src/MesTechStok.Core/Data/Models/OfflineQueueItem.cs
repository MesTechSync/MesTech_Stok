using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// OpenCart ve diğer entegrasyon kanalları için offline kuyruğa alınmış işler.
    /// Dayanıklılık (resilience) tasarımına uygun olarak API kesintilerinde veriyi güvenle biriktirir.
    /// </summary>
    public class OfflineQueueItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Kanal adı: Product, Stock, Orders, Categories, Generic
        /// </summary>
        [Required]
        [MaxLength(32)]
        public string Channel { get; set; } = "Generic";

        /// <summary>
        /// Yön: In/Out (Import/Export)
        /// </summary>
        [Required]
        [MaxLength(16)]
        public string Direction { get; set; } = "Out";

        /// <summary>
        /// JSON içerik (idempotent tekrarlar için gerekli alanlarla birlikte)
        /// </summary>
        public string? Payload { get; set; }

        /// <summary>
        /// Durum: Pending, Processing, Failed, Succeeded
        /// </summary>
        [Required]
        [MaxLength(16)]
        public string Status { get; set; } = "Pending";

        public int RetryCount { get; set; } = 0;

        public DateTime? NextAttemptAt { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(4000)]
        public string? LastError { get; set; }

        [MaxLength(64)]
        public string? CorrelationId { get; set; }
    }
}


