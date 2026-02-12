using System;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models
{
    public class LogEntry
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Level { get; set; } = "Info"; // Info, Warning, Error, Debug

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "General"; // Product, User, System, Security, Performance

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = "";

        public string? Data { get; set; } // JSON formatted data

        [MaxLength(100)]
        public string? UserId { get; set; }

        public string? Exception { get; set; } // Exception details

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(100)]
        public string? MachineName { get; set; }

        // Indexes for better performance
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
