namespace MesTech.Domain.Enums;

/// <summary>
/// Genel muhasebe hareket tipi. GLTransaction entity'si tarafından kullanılır.
/// CariHareket için kullanılan TransactionType'dan farklıdır.
/// </summary>
public enum GLTransactionType { Income, Expense, Transfer, Refund }
