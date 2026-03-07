using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Tedarikçi entity'si.
/// </summary>
public class Supplier : BaseEntity
{
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
    public decimal CurrentBalance { get; set; }
    public decimal? DiscountRate { get; set; }
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; set; } = true;
    public bool IsPreferred { get; set; }
    public int? Rating { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public string? Notes { get; set; }
    public string? DocumentUrls { get; set; }

    // Navigation
    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    public override string ToString() => $"[{Code}] {Name}";
}
