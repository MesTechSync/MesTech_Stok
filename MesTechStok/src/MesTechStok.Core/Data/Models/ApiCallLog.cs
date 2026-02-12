using System;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models
{
    public class ApiCallLog
    {
        [Key]
        public long Id { get; set; }
        [Required]
        [MaxLength(256)]
        public string Endpoint { get; set; } = string.Empty;
        [Required]
        [MaxLength(10)]
        public string Method { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int? StatusCode { get; set; }
        [MaxLength(32)]
        public string? Category { get; set; }
        public long DurationMs { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        [MaxLength(64)]
        public string? CorrelationId { get; set; }
    }
}
