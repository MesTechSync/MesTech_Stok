using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Persistence.Accounting;

/// <summary>
/// PostgreSQL Row-Level Security (RLS) policy kurulumu.
/// Muhasebe tablolarinda tenant izolasyonu icin RLS aktive eder.
/// EF Core Global Query Filter'in yaninda ikinci katman guvenlik saglar.
/// </summary>
public static class RlsPolicySetup
{
    /// <summary>
    /// Muhasebe tablolarina RLS policy uygular.
    /// Idempotent: mevcut policy varsa DROP + CREATE yapar.
    /// </summary>
    public static async Task ApplyRlsPoliciesAsync(DbContext context, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tables = new[]
        {
            "\"JournalEntries\"",
            "\"JournalLines\"",
            "\"SettlementBatches\"",
            "\"AccountingBankTransactions\"",
            "\"PersonalExpenses\"",
            "\"ReconciliationMatches\"",
            "\"ChartOfAccounts\"",
            "\"Counterparties\"",
            "\"CommissionRecords\"",
            "\"CargoExpenses\"",
            "\"AccountingDocuments\"",
            "\"CashFlowEntries\"",
            "\"TaxRecords\"",
            "\"TaxWithholdings\"",
            "\"ProfitReports\"",
            "\"FinancialGoals\"",
            "\"AccountingExpenseCategories\"",
            "\"AccountingSupplierAccounts\""
        };

        foreach (var table in tables)
        {
            var policyName = $"tenant_isolation_{table.Replace("\"", "").ToLowerInvariant()}";

            var sql = $"""
                -- Enable RLS on table (idempotent)
                ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;

                -- Force RLS for table owner as well
                ALTER TABLE {table} FORCE ROW LEVEL SECURITY;

                -- Drop existing policy if present (idempotent)
                DROP POLICY IF EXISTS {policyName} ON {table};

                -- Create tenant isolation policy
                -- current_setting('app.current_tenant_id') is set by TenantContextInterceptor
                CREATE POLICY {policyName} ON {table}
                    USING ("TenantId"::text = current_setting('app.current_tenant_id', true))
                    WITH CHECK ("TenantId"::text = current_setting('app.current_tenant_id', true));
                """;

            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
                logger?.LogInformation("RLS policy applied: {PolicyName} on {Table}", policyName, table);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to apply RLS policy on {Table} — may require superuser privileges", table);
            }
        }

        logger?.LogInformation("RLS policy setup completed for {Count} accounting tables", tables.Length);
    }
}
