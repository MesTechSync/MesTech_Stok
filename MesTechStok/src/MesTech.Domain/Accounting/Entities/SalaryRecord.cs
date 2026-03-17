using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Maas kaydi — calisan bazinda aylik maas detaylari ve vergi/SGK hesaplamalari.
/// </summary>
public class SalaryRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public Guid? EmployeeId { get; private set; }
    public decimal GrossSalary { get; private set; }
    public decimal SGKEmployer { get; private set; }
    public decimal SGKEmployee { get; private set; }
    public decimal IncomeTax { get; private set; }
    public decimal StampTax { get; private set; }
    public decimal NetSalary { get; private set; }
    public decimal TotalEmployerCost => GrossSalary + SGKEmployer;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? PaidDate { get; private set; }
    public string? Notes { get; private set; }

    private SalaryRecord() { }

    public static SalaryRecord Create(
        Guid tenantId,
        string employeeName,
        decimal grossSalary,
        decimal sgkEmployer,
        decimal sgkEmployee,
        decimal incomeTax,
        decimal stampTax,
        int year,
        int month,
        Guid? employeeId = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeName);
        if (grossSalary <= 0)
            throw new ArgumentOutOfRangeException(nameof(grossSalary), "Gross salary must be positive.");
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2100.");
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        var netSalary = grossSalary - sgkEmployee - incomeTax - stampTax;

        return new SalaryRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeName = employeeName,
            EmployeeId = employeeId,
            GrossSalary = grossSalary,
            SGKEmployer = sgkEmployer,
            SGKEmployee = sgkEmployee,
            IncomeTax = incomeTax,
            StampTax = stampTax,
            NetSalary = netSalary,
            Year = year,
            Month = month,
            PaymentStatus = PaymentStatus.Pending,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsPaid(DateTime? paidDate = null)
    {
        PaymentStatus = PaymentStatus.Completed;
        PaidDate = paidDate ?? DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePaymentStatus(PaymentStatus status)
    {
        PaymentStatus = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
