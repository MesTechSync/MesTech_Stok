using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KOK2_MissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: add columns only if not already present
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Stores' AND column_name='CurrentAccountBalance' AND data_type='numeric' AND numeric_precision IS NULL) THEN
        ALTER TABLE ""Stores"" ALTER COLUMN ""CurrentAccountBalance"" TYPE numeric(18,2);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Stores' AND column_name='AutoSyncInvoice') THEN
        ALTER TABLE ""Stores"" ADD COLUMN ""AutoSyncInvoice"" boolean NOT NULL DEFAULT false;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='StoreCredentials' AND column_name='EncryptedCredential') THEN
        ALTER TABLE ""StoreCredentials"" ADD COLUMN ""EncryptedCredential"" text;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Invoices' AND column_name='TenantNo') THEN
        ALTER TABLE ""Invoices"" ADD COLUMN ""TenantNo"" text;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Categories' AND column_name='InternalCategoryPath') THEN
        ALTER TABLE ""Categories"" ADD COLUMN ""InternalCategoryPath"" text;
    END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoSyncInvoice",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "EncryptedCredential",
                table: "StoreCredentials");

            migrationBuilder.DropColumn(
                name: "TenantNo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InternalCategoryPath",
                table: "Categories");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentAccountBalance",
                table: "Stores",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);
        }
    }
}
