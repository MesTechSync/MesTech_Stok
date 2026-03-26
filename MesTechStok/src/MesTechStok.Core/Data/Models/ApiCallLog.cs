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
        public bool IsSuccess { get => Success; set => Success = value; }
        public int? StatusCode { get; set; }
        [MaxLength(32)]
        public string? Category { get; set; }
        public long DurationMs { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        [MaxLength(64)]
        public string? CorrelationId { get; set; }
        [MaxLength(2000)]
        public string? RequestBody { get; set; }
        [MaxLength(2000)]
        public string? ResponseBody { get; set; }
        [MaxLength(500)]
        public string? ErrorMessage { get; set; }
    }
}
