using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Adapter for syncing financial data to an external ERP system (e.g. Parasut, Logo, Mikro).
/// Each ERP provider implements this interface.
/// </summary>
public interface IERPAdapter
{
    /// <summary>
    /// ERP system name (e.g. "Parasut", "Logo", "Mikro").
    /// </summary>
    string ERPName { get; }

    /// <summary>
    /// Tests the connection to the ERP system.
    /// Returns true if the ERP API is reachable and credentials are valid.
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Syncs invoices to the ERP system (creates sales_invoices in the ERP).
    /// </summary>
    Task SyncInvoicesAsync(IReadOnlyList<Invoice> invoices, CancellationToken ct = default);

    /// <summary>
    /// Syncs expenses to the ERP system (creates purchase_bills in the ERP).
    /// </summary>
    Task SyncExpensesAsync(IReadOnlyList<AccountingExpenseDto> expenses, CancellationToken ct = default);

    /// <summary>
    /// Syncs counterparties (customers/suppliers) to the ERP system.
    /// </summary>
    Task SyncCounterpartiesAsync(IReadOnlyList<CounterpartyDto> parties, CancellationToken ct = default);

    /// <summary>
    /// Gets the balance for a specific GL account code from the ERP.
    /// </summary>
    Task<decimal> GetBalanceAsync(string accountCode, CancellationToken ct = default);
}

/// <summary>
/// Factory for resolving ERP adapters by name.
/// </summary>
public interface IERPAdapterFactory
{
    /// <summary>
    /// Gets the adapter for the given ERP system (case-insensitive).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if ERP is not supported.</exception>
    IERPAdapter GetAdapter(string erpName);

    /// <summary>
    /// List of supported ERP system names.
    /// </summary>
    IReadOnlyList<string> SupportedERPs { get; }
}
