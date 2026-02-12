using System;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Circuit breaker durum geçiş kaydı - operasyonel görünürlük için
    /// Her OPEN/CLOSED/HALF_OPEN değişikliği burada izlenir
    /// </summary>
    public class CircuitStateLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string PreviousState { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NewState { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;

        public double FailureRate { get; set; }
        public int WindowTotalCalls { get; set; }
        public DateTime TransitionTimeUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(64)]
        public string? CorrelationId { get; set; }

        [MaxLength(256)]
        public string? AdditionalInfo { get; set; }
    }
}
