using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Salary Record data transfer object.
/// </summary>
public class SalaryRecordDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal SGKEmployer { get; set; }
    public decimal SGKEmployee { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal StampTax { get; set; }
    public decimal NetSalary { get; set; }
    public decimal TotalEmployerCost { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
