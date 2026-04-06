using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Consolidation migration — April 2026.
    /// xmin columns are PostgreSQL system columns (NOT user-created).
    /// EF Core tracks them as shadow properties but they must NOT be ADD/DROP'd.
    /// This migration only applies real schema changes: FK, indexes, new columns.
    /// </summary>
    public partial class Consolidate_XminAndModelSync_April2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Fix User→Tenant FK: SetNull → Restrict (TenantId is required)
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            // 2. Add RowVersion columns where entity declares them (idempotent)
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductWarehouseStocks' AND column_name='RowVersion') THEN
                        ALTER TABLE ""ProductWarehouseStocks"" ADD COLUMN ""RowVersion"" bytea;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PenaltyRecords' AND column_name='RowVersion') THEN
                        ALTER TABLE ""PenaltyRecords"" ADD COLUMN ""RowVersion"" bytea;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PaymentTransactions' AND column_name='RowVersion') THEN
                        ALTER TABLE ""PaymentTransactions"" ADD COLUMN ""RowVersion"" bytea;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='JournalEntries' AND column_name='RowVersion') THEN
                        ALTER TABLE ""JournalEntries"" ADD COLUMN ""RowVersion"" bytea;
                    END IF;
                END $$;
            ");

            // 3. CommissionRecords → SettlementBatch FK (new relationship)
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CommissionRecords' AND column_name='SettlementBatchId') THEN
                        ALTER TABLE ""CommissionRecords"" ADD COLUMN ""SettlementBatchId"" uuid;
                    END IF;
                END $$;
            ");

            // 4. Create indexes (idempotent — IF NOT EXISTS)
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_SettlementLines_Tenant_Batch"" ON ""SettlementLines"" (""TenantId"", ""SettlementBatchId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PlatformPayments_Tenant_Store"" ON ""PlatformPayments"" (""TenantId"", ""StoreId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_JournalLines_Tenant_Account"" ON ""JournalLines"" (""TenantId"", ""AccountId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_DunningLogs_Tenant_Subscription"" ON ""DunningLogs"" (""TenantId"", ""TenantSubscriptionId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_CommissionRecords_SettlementBatchId"" ON ""CommissionRecords"" (""SettlementBatchId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_CashFlowEntries_Tenant_Counterparty"" ON ""CashFlowEntries"" (""TenantId"", ""CounterpartyId"");");

            // 5. CommissionRecords → SettlementBatches FK
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name='FK_CommissionRecords_SettlementBatches_SettlementBatchId') THEN
                        ALTER TABLE ""CommissionRecords"" ADD CONSTRAINT ""FK_CommissionRecords_SettlementBatches_SettlementBatchId""
                            FOREIGN KEY (""SettlementBatchId"") REFERENCES ""SettlementBatches"" (""Id"");
                    END IF;
                END $$;
            ");

            // 6. Users→Tenants FK with Restrict
            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommissionRecords_SettlementBatches_SettlementBatchId",
                table: "CommissionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(name: "IX_SettlementLines_Tenant_Batch", table: "SettlementLines");
            migrationBuilder.DropIndex(name: "IX_PlatformPayments_Tenant_Store", table: "PlatformPayments");
            migrationBuilder.DropIndex(name: "IX_JournalLines_Tenant_Account", table: "JournalLines");
            migrationBuilder.DropIndex(name: "IX_DunningLogs_Tenant_Subscription", table: "DunningLogs");
            migrationBuilder.DropIndex(name: "IX_CommissionRecords_SettlementBatchId", table: "CommissionRecords");
            migrationBuilder.DropIndex(name: "IX_CashFlowEntries_Tenant_Counterparty", table: "CashFlowEntries");

            migrationBuilder.DropColumn(name: "SettlementBatchId", table: "CommissionRecords");
            migrationBuilder.DropColumn(name: "RowVersion", table: "ProductWarehouseStocks");
            migrationBuilder.DropColumn(name: "RowVersion", table: "PenaltyRecords");
            migrationBuilder.DropColumn(name: "RowVersion", table: "PaymentTransactions");
            migrationBuilder.DropColumn(name: "RowVersion", table: "JournalEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
