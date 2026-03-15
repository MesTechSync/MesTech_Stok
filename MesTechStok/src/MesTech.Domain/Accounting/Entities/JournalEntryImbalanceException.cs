using MesTech.Domain.Exceptions;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Yevmiye kaydi dengesizlik istisnasi.
/// </summary>
public class JournalEntryImbalanceException : DomainException
{
    public decimal TotalDebit { get; }
    public decimal TotalCredit { get; }

    public JournalEntryImbalanceException(decimal totalDebit, decimal totalCredit)
        : base($"Journal entry is imbalanced: total debit ({totalDebit:N2}) != total credit ({totalCredit:N2}).")
    {
        TotalDebit = totalDebit;
        TotalCredit = totalCredit;
    }
}
