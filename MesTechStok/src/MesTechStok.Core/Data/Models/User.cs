using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Kullanıcı varlığı - Authentication ve Authorization için temel model
/// </summary>
[Obsolete("MIGRATE: Use MesTech.Domain.Entities.User (Guid PK). Bridge via ExternalId property. Will be removed in Dalga 6.")]
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Domain.User (Guid PK) ile korrelasyon köprüsü.
    /// Yeni kod Domain entity kullanır — bu alan data migration için.
    /// </summary>
    public Guid ExternalId { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Email { get; set; }

    [Required]
    [StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; } = false;
    public DateTime? LastLoginDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Kullanıcının tam adı (DB column)
    /// </summary>
    [StringLength(200)]
    public string? FullName { get; set; }

    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}