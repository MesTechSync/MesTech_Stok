using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// KDV Beyannamesi — aylik KDV hesaplama ve GIB'e gonderim.
/// GL entry'lerden otomatik hesaplanir: 391 (tahsil edilen) - 191 (odenen).
/// </summary>
public sealed class VatDeclaration : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal TotalSales { get; private set; }
    public decimal TotalVatCollected { get; private set; }
    public decimal TotalVatPaid { get; private set; }
    public decimal NetVatPayable { get; private set; }
    public VatDeclarationStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public string? GibReferenceNumber { get; private set; }
    public string? Notes { get; private set; }

    private VatDeclaration() { }

    public static VatDeclaration Create(Guid tenantId, int year, int month)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        if (year < 2020 || year > 2099)
            throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month));

        return new VatDeclaration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = year,
            Month = month,
            Status = VatDeclarationStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Calculate(decimal sales, decimal vatCollected, decimal vatPaid)
    {
        if (Status == VatDeclarationStatus.Submitted || Status == VatDeclarationStatus.Accepted)
            throw new InvalidOperationException("Gonderilmis beyanname tekrar hesaplanamaz.");

        TotalSales = sales;
        TotalVatCollected = vatCollected;
        TotalVatPaid = vatPaid;
        NetVatPayable = vatCollected - vatPaid;
        Status = VatDeclarationStatus.Calculated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Submit(string gibReferenceNumber)
    {
        if (Status != VatDeclarationStatus.Calculated)
            throw new InvalidOperationException("Sadece hesaplanmis beyanname gonderilebilir.");
        ArgumentException.ThrowIfNullOrWhiteSpace(gibReferenceNumber);

        GibReferenceNumber = gibReferenceNumber;
        Status = VatDeclarationStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAccepted() { Status = VatDeclarationStatus.Accepted; UpdatedAt = DateTime.UtcNow; }
    public void MarkRejected(string? reason) { Status = VatDeclarationStatus.Rejected; Notes = reason; UpdatedAt = DateTime.UtcNow; }
}

public enum VatDeclarationStatus
{
    Draft = 0,
    Calculated = 1,
    Submitted = 2,
    Accepted = 3,
    Rejected = 4
}
