using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Sabit gider kaydi — aylik tekrarlayan giderler (kira, internet, sigorta vb.).
/// </summary>
public class FixedExpense : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public decimal MonthlyAmount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public int DayOfMonth { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public string? SupplierName { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string? Notes { get; private set; }

    private FixedExpense() { }

    public static FixedExpense Create(
        Guid tenantId,
        string name,
        decimal monthlyAmount,
        int dayOfMonth,
        DateTime startDate,
        string currency = "TRY",
        DateTime? endDate = null,
        string? supplierName = null,
        Guid? supplierId = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (monthlyAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyAmount), "Monthly amount must be positive.");
        if (dayOfMonth < 1 || dayOfMonth > 31)
            throw new ArgumentOutOfRangeException(nameof(dayOfMonth), "Day of month must be between 1 and 31.");

        return new FixedExpense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            MonthlyAmount = monthlyAmount,
            Currency = currency,
            DayOfMonth = dayOfMonth,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            SupplierName = supplierName,
            SupplierId = supplierId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        EndDate ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        EndDate = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAmount(decimal newAmount)
    {
        if (newAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(newAmount), "Amount must be positive.");
        MonthlyAmount = newAmount;
        UpdatedAt = DateTime.UtcNow;
    }
}
