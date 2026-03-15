namespace MesTech.Application.DTOs.Accounting;

public class BankTransactionDto
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public bool IsReconciled { get; set; }
}
