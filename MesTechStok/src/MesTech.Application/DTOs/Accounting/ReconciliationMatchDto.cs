namespace MesTech.Application.DTOs.Accounting;

public class ReconciliationMatchDto
{
    public Guid Id { get; set; }
    public Guid? SettlementBatchId { get; set; }
    public Guid? BankTransactionId { get; set; }
    public DateTime MatchDate { get; set; }
    public decimal Confidence { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
