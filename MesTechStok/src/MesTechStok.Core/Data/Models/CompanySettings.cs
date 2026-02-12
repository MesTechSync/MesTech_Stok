using System;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Şirket ve çoklu depo/şube ayarlarının kalıcı olarak tutulduğu tablo
    /// Tek kayıt mantığıyla çalışır. Gerekirse birden fazla profil de desteklenebilir.
    /// </summary>
    public class CompanySettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(1000)]
        public string? Address { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}


