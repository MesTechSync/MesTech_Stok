using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Penalty Record data transfer object.
/// </summary>
public sealed class PenaltyRecordDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public PenaltySource Source { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime PenaltyDate { get; set; }
    public DateTime? DueDate { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid? RelatedOrderId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
