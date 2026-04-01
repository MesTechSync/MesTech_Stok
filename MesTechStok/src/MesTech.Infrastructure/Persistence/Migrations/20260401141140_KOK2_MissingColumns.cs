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
            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentAccountBalance",
                table: "Stores",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSyncInvoice",
                table: "Stores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedCredential",
                table: "StoreCredentials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantNo",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalCategoryPath",
                table: "Categories",
                type: "text",
                nullable: true);
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
