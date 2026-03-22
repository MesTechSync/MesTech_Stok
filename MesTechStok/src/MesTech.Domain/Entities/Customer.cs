using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Müşteri entity'si.
/// </summary>
public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CustomerType { get; set; } = "INDIVIDUAL";
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? VatNumber { get; set; }
    public string? IdentityNumber { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal CurrentBalance { get; private set; }
    public decimal? DiscountRate { get; set; }
    public int PaymentTermDays { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Segment { get; set; }
    public int? Rating { get; set; }
    public bool IsVip { get; private set; }
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; private set; }
    public string? BlockReason { get; private set; }
    public DateTime? LastOrderDate { get; private set; }
    public DateTime? BirthDate { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredContactMethod { get; set; }
    public bool AcceptsMarketing { get; set; }
    public string? Website { get; set; }
    public string? FacebookProfile { get; set; }
    public string? InstagramProfile { get; set; }
    public string? LinkedInProfile { get; set; }
    public string? Notes { get; set; }
    public string? DocumentUrls { get; set; }

    // Concurrency
    public byte[]? RowVersion { get; set; }

    // Navigation
    private readonly List<Order> _orders = new();
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    // ── Domain Logic ──

    public void Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Block reason is required.", nameof(reason));
        IsBlocked = true;
        BlockReason = reason;
    }

    public void Unblock()
    {
        IsBlocked = false;
        BlockReason = null;
    }

    public void AdjustBalance(decimal amount)
    {
        CurrentBalance += amount;
    }

    public void RecordOrderPlaced()
    {
        LastOrderDate = DateTime.UtcNow;
    }

    public void PromoteToVip() => IsVip = true;
    public void DemoteFromVip() => IsVip = false;

    public bool HasExceededCreditLimit => CreditLimit.HasValue && CurrentBalance > CreditLimit.Value;

    public string DisplayName => string.IsNullOrWhiteSpace(ContactPerson) ? Name : $"{Name} ({ContactPerson})";

    public override string ToString() => $"[{Code}] {Name}";
}
