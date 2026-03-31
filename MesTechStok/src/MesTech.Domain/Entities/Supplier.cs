using MesTech.Domain.Common;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Tedarikçi entity'si.
/// </summary>
public sealed class Supplier : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Fax { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? VatNumber { get; set; }
    public string? TradeRegisterNumber { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal CurrentBalance { get; private set; }
    public decimal? DiscountRate { get; set; }
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; set; } = true;
    public bool IsPreferred { get; private set; }
    public int? Rating { get; private set; }
    public DateTime? LastOrderDate { get; private set; }
    public string? Notes { get; set; }
    public string? DocumentUrls { get; set; }

    // ── Muhasebe Modulu (MUH-01) ──
    public DateTime? LastPaymentDate { get; private set; }

    // Navigation
    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    // ── Domain Logic ──

    public void AdjustBalance(decimal amount)
    {
        if (amount == 0)
            throw new ArgumentException("Adjustment amount cannot be zero.", nameof(amount));
        CurrentBalance += amount;
    }

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Ödeme tutarı pozitif olmalı.");
        CurrentBalance -= amount;
        LastPaymentDate = DateTime.UtcNow;
    }

    public void RecordOrderPlaced()
    {
        LastOrderDate = DateTime.UtcNow;
    }

    public void SetRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        Rating = rating;
    }

    public void MarkAsPreferred() => IsPreferred = true;
    public void UnmarkAsPreferred() => IsPreferred = false;

    public bool HasExceededCreditLimit => CreditLimit.HasValue && CurrentBalance > CreditLimit.Value;

    // ── Factory ──

    public static Supplier Create(Guid tenantId, string name, string code, string? email = null, string? phone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            Email = email,
            Phone = phone
        };

        supplier.RaiseDomainEvent(new SupplierCreatedEvent(
            supplier.Id, tenantId, name, code, DateTime.UtcNow));

        return supplier;
    }

    public override string ToString() => $"[{Code}] {Name}";
}
