using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncSnapshot_Dalga15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountTransactions_CustomerAccounts_CustomerAccountId",
                table: "AccountTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountTransactions_SupplierAccounts_SupplierAccountId",
                table: "AccountTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_CategoryId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerAccounts_Customers_CustomerId",
                table: "CustomerAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Leaves_Employees_EmployeeId",
                table: "Leaves");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformPayments_Stores_StoreId",
                table: "PlatformPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Customers_CustomerId",
                table: "Quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequestLines_Products_ProductId",
                table: "ReturnRequestLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequests_Orders_OrderId",
                table: "ReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequests_Stores_StoreId",
                table: "ReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierAccounts_Suppliers_SupplierId",
                table: "SupplierAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_ProductSetItems_ProductSetId",
                table: "ProductSetItems");

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PlatformCommissions_Tenant_Platform_Category",
                table: "PlatformCommissions");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CategoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_AccountTransactions_CustomerAccountId",
                table: "AccountTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AccountTransactions_SupplierAccountId",
                table: "AccountTransactions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CustomerAccountId",
                table: "AccountTransactions");

            migrationBuilder.DropColumn(
                name: "SupplierAccountId",
                table: "AccountTransactions");

            migrationBuilder.RenameIndex(
                name: "IX_Quotations_TenantId_Status",
                table: "Quotations",
                newName: "IX_Quotations_Tenant_Status");

            migrationBuilder.RenameIndex(
                name: "IX_KontorBalances_StoreId_Provider",
                table: "KontorBalances",
                newName: "IX_KontorBalances_Store_Provider");

            migrationBuilder.RenameIndex(
                name: "IX_ErpSyncLogs_TenantId",
                table: "erp_sync_logs",
                newName: "IX_erp_sync_logs_TenantId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "WorkTasks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemperatureRange",
                table: "WarehouseZones",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WarehouseZones",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HumidityRange",
                table: "WarehouseZones",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseZones",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingSection",
                table: "WarehouseZones",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WarehouseZones",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WarehouseShelves",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseShelves",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Accessibility",
                table: "WarehouseShelves",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WarehouseShelves",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "UsableArea",
                table: "Warehouses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Warehouses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalArea",
                table: "Warehouses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Warehouses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Warehouses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Warehouses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OperatingHours",
                table: "Warehouses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Warehouses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Warehouses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyCost",
                table: "Warehouses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinTemperature",
                table: "Warehouses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinHumidity",
                table: "Warehouses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxTemperature",
                table: "Warehouses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxHumidity",
                table: "Warehouses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxCapacity",
                table: "Warehouses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Height",
                table: "Warehouses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Warehouses",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Warehouses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Warehouses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPerSquareMeter",
                table: "Warehouses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CostCenter",
                table: "Warehouses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPerson",
                table: "Warehouses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Warehouses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Warehouses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CapacityUnit",
                table: "Warehouses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Warehouses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RackType",
                table: "WarehouseRacks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Orientation",
                table: "WarehouseRacks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WarehouseRacks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseRacks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WarehouseRacks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WarehouseBins",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseBins",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BinType",
                table: "WarehouseBins",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WarehouseBins",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMfaEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfaEnabledAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecret",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "UserRoles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TimeEntries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyAmount",
                table: "TaxRecords",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "TaxRecords",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "TaxRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SyncType",
                table: "SyncRetryItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LastError",
                table: "SyncRetryItems",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ItemType",
                table: "SyncRetryItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                table: "SyncRetryItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorCategory",
                table: "SyncRetryItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "SyncRetryItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalInfo",
                table: "SyncRetryItems",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlatformCode",
                table: "SyncLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "SyncLogs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "SyncLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "SyncLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "SyncLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "Suppliers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VatNumber",
                table: "Suppliers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TradeRegisterNumber",
                table: "Suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxOffice",
                table: "Suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxNumber",
                table: "Suppliers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Suppliers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Suppliers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Suppliers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "Suppliers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Fax",
                table: "Suppliers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentUrls",
                table: "Suppliers",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountRate",
                table: "Suppliers",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Suppliers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentBalance",
                table: "Suppliers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Suppliers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditLimit",
                table: "Suppliers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPerson",
                table: "Suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Suppliers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierTaxOffice",
                table: "SupplierAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierTaxNumber",
                table: "SupplierAccounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierPhone",
                table: "SupplierAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierName",
                table: "SupplierAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierEmail",
                table: "SupplierAccounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierAddress",
                table: "SupplierAccounts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SupplierAccounts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AccountCode",
                table: "SupplierAccounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Reasoning",
                table: "StockPredictions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "StockMovements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCost",
                table: "StockMovements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "StockMovements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProcessedBy",
                table: "StockMovements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "StockMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductSKU",
                table: "StockMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastError",
                table: "SocialFeedConfigurations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FeedUrl",
                table: "SocialFeedConfigurations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryFilter",
                table: "SocialFeedConfigurations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "Sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "Sessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceInfo",
                table: "Sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrackingNumber",
                table: "ReturnRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReasonDetail",
                table: "ReturnRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlatformReturnId",
                table: "ReturnRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ReturnRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "ReturnRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "ReturnRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "ReturnRequests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ReturnRequests",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "ReturnRequestLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "ReturnRequestLines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RefundAmount",
                table: "ReturnRequestLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "ReturnRequestLines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ReturnRequestLines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Terms",
                table: "Quotations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Quotations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerTaxOffice",
                table: "Quotations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerTaxNumber",
                table: "Quotations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Quotations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerAddress",
                table: "Quotations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "QuotationLines",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "QuotationLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "ProjectMembers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "FulfillmentCenter",
                table: "ProductWarehouseStocks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ProductSets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProductSetItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "Products",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRate",
                table: "Products",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "SalePrice",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchasePrice",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ListPrice",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Strategy",
                table: "PriceRecommendations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "PriceRecommendations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Reasoning",
                table: "PriceRecommendations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PlatformPaymentId",
                table: "PlatformPayments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PlatformPayments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PlatformPayments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BankReference",
                table: "PlatformPayments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "PlatformCommissions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PlatformCategoryId",
                table: "PlatformCommissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PlatformCommissions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PlatformCommissions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "PlatformCommissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Probability",
                table: "PipelineStages",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Permissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Module",
                table: "Permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "PaymentTransactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PaymentTransactions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PaymentTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "TrackingNumber",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotal",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "PlatformOrderNumber",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalOrderId",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionRate",
                table: "Orders",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CargoExpenseAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CargoBarcode",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhone",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "OrderItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRate",
                table: "OrderItems",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "OrderItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "ProductSKU",
                table: "OrderItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "OrderItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OfflineQueueItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LastError",
                table: "OfflineQueueItems",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                table: "OfflineQueueItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "OfflineQueueItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Channel",
                table: "OfflineQueueItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateName",
                table: "NotificationLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Recipient",
                table: "NotificationLogs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "NotificationLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "NotificationLogs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LogEntries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "LogEntries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "LogEntries",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "MachineName",
                table: "LogEntries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "LogEntries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "LogEntries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Exception",
                table: "LogEntries",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "LogEntries",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "LogEntries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "LogEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Leaves",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Leaves",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Leads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoreReasoning",
                table: "Leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScoredAt",
                table: "Leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "JournalEntries",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxTotal",
                table: "Invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotal",
                table: "Invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "PdfUrl",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GrandTotal",
                table: "Invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Invoices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivityCode",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CargoShipmentId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomsDeclarationNo",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverSurname",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExemptionCode",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExportCurrency",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExportExchangeRate",
                table: "Invoices",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GibStatus",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GibStatusDate",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GtipCode",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParasutEInvoiceId",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParasutSalesInvoiceId",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParasutSyncError",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParasutSyncStatus",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ParasutSyncedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalTitle",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scenario",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentAddress",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShipmentDate",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignatureStatus",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SignatureType",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedBy",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiclePlate",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaybillNumber",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingAmount",
                table: "Invoices",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingRate",
                table: "Invoices",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "InvoiceLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRate",
                table: "InvoiceLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "InvoiceLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "LineTotal",
                table: "InvoiceLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "InvoiceLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingQty",
                table: "InventoryLots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ReceivedQty",
                table: "InventoryLots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "LotNumber",
                table: "InventoryLots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Incomes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionAmount",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Incomes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "Incomes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCost",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Incomes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "FinanceExpenses",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "FinanceExpenses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "FinanceExpenses",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "FinanceExpenses",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "RecurrencePeriod",
                table: "Expenses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Expenses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Expenses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Expenses",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Expenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Expenses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "Expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "erp_sync_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "erp_sync_logs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ErrorDetails",
                table: "erp_sync_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailCount",
                table: "erp_sync_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SkipCount",
                table: "erp_sync_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SuccessCount",
                table: "erp_sync_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRecords",
                table: "erp_sync_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TriggeredBy",
                table: "erp_sync_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "e_invoice_send_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "e_invoice_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "e_invoice_documents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingAmount",
                table: "e_invoice_documents",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingRate",
                table: "e_invoice_documents",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WebsiteUrl",
                table: "DropshipSuppliers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DropshipSuppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "MarkupValue",
                table: "DropshipSuppliers",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "DropshipSuppliers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApiEndpoint",
                table: "DropshipSuppliers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "DropshipProducts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "SellingPrice",
                table: "DropshipProducts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalPrice",
                table: "DropshipProducts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalUrl",
                table: "DropshipProducts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalProductId",
                table: "DropshipProducts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ReliabilityColor",
                table: "DropshippingPoolProducts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ReliabilityScore",
                table: "DropshippingPoolProducts",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierTrackingNumber",
                table: "DropshipOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierOrderRef",
                table: "DropshipOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FailureReason",
                table: "DropshipOrders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxOffice",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxNumber",
                table: "Customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BillingAddress",
                table: "Customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerTaxOffice",
                table: "CustomerAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerTaxNumber",
                table: "CustomerAccounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "CustomerAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "CustomerAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "CustomerAccounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerAddress",
                table: "CustomerAccounts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "CustomerAccounts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditLimit",
                table: "CustomerAccounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "AccountCode",
                table: "CustomerAccounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomerAccounts",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxOffice",
                table: "CrmContacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "CrmContacts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "CrmContacts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CrmContacts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxNumber",
                table: "CompanySettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "CompanySettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "CompanySettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "CompanySettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CompanySettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSyncInvoice",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSyncStock",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ErpProvider",
                table: "CompanySettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsErpConnected",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PriceSyncPeriodMinutes",
                table: "CompanySettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockSyncPeriodMinutes",
                table: "CompanySettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "RateSource",
                table: "CommissionRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "CircuitStateLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PreviousState",
                table: "CircuitStateLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NewState",
                table: "CircuitStateLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "CircuitStateLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalInfo",
                table: "CircuitStateLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CircuitStateLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "InternalCategoryPath",
                table: "CategoryPlatformMappings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CategoryPlatformMappings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoMapped",
                table: "CategoryPlatformMappings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MappedAt",
                table: "CategoryPlatformMappings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MappedBy",
                table: "CategoryPlatformMappings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MatchConfidence",
                table: "CategoryPlatformMappings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CategoryPlatformMappings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlatformCategoryPath",
                table: "CategoryPlatformMappings",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Categories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Categories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Categories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CariHareketler",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CalendarEvents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "CalendarEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "CalendarEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderDate",
                table: "CalendarEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CalendarEventAttendees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Bitrix24DealProductRows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "ValidationMessage",
                table: "BarcodeScanLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "BarcodeScanLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "BarcodeScanLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "BarcodeScanLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "BarcodeScanLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "BarcodeScanLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "BarcodeScanLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "ApiCallLogs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Endpoint",
                table: "ApiCallLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "ApiCallLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ApiCallLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApiCallLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "DocumentNumber",
                table: "AccountTransactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AccountTransactions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "AccountTransactions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "AccountingDocuments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AccessLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Resource",
                table: "AccessLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "AccessLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "AccessLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalInfo",
                table: "AccessLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AccessLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AccessLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldValues = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    NewValues = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BaBsRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CounterpartyVkn = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    CounterpartyName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DocumentCount = table.Column<int>(type: "integer", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaBsRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlannedRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlannedExpense = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualExpense = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Variance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PlatformType = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cash_registers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_registers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CiceksepetiCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CiceksepetiCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ParentCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    IsLeaf = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CiceksepetiCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "erp_conflict_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MestechValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ErpValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Winner = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Auto"),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_erp_conflict_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpAccountMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MesTechAccountCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MesTechAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MesTechAccountType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErpAccountCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErpAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpAccountMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AssetCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AcquisitionCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AcquisitionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetBookValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FixedExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedExpenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FulfillmentShipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Center = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CarrierCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FulfillmentShipments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HepsiburadaListing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HepsiburadaSKU = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MerchantSKU = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ListingStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HepsiburadaListing", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KvkkAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AffectedRecordCount = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KvkkAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PointsPerPurchase = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    MinRedeemPoints = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    ChannelAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnOrderReceived = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnLowStock = table.Column<bool>(type: "boolean", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: false),
                    NotifyOnInvoiceDue = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnPaymentReceived = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnPlatformMessage = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnAIInsight = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnBuyboxLost = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnSystemError = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnTaxDeadline = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnReportReady = table.Column<bool>(type: "boolean", nullable: false),
                    QuietHoursStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    QuietHoursEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DigestMode = table.Column<bool>(type: "boolean", nullable: false),
                    DigestTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    CompletedStepsJson = table.Column<string>(type: "jsonb", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_onboarding_progress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PenaltyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PenaltyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalDataRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityTypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    AnonymizationStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FieldsToAnonymize = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalDataRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalConversationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AiSuggestedReply = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Reply = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RepliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RepliedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: true),
                    OldPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OldListPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NewListPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ChangedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedDomainEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HandlerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedDomainEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfitLossEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RevenueAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpenseAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitLossEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecurringExpense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NextDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringExpense", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrossSalary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SGKEmployer = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SGKEmployee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IncomeTax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StampTax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetSalary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalEmployerCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FilterJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    LastExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DesiWeight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CargoBarcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsChargedToCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerChargeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentCosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockAlertRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarningThreshold = table.Column<int>(type: "integer", nullable: false),
                    CriticalThreshold = table.Column<int>(type: "integer", nullable: false),
                    AutoReorderEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReorderQuantity = table.Column<int>(type: "integer", nullable: true),
                    PreferredSupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAlertRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAlertRules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlertLevel = table.Column<int>(type: "integer", nullable: false),
                    CurrentStock = table.Column<int>(type: "integer", nullable: false),
                    ThresholdStock = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AlertDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAlerts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLots_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLots_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StockPlacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShelfId = table.Column<Guid>(type: "uuid", nullable: true),
                    BinId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShelfCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BinCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    MinimumStock = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ProductSku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPlacements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPlacements_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MaxStores = table.Column<int>(type: "integer", nullable: false),
                    MaxProducts = table.Column<int>(type: "integer", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    FeaturesJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeadLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RawBody = table.Column<string>(type: "text", nullable: false),
                    Signature = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProcessedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeadLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampaignProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignProducts_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cash_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashRegisterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedCurrentAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_transactions_cash_registers_CashRegisterId",
                        column: x => x.CashRegisterId,
                        principalTable: "cash_registers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportFieldMapping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceColumn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetField = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportFieldMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportFieldMapping_ImportTemplate_ImportTemplateId",
                        column: x => x.ImportTemplateId,
                        principalTable: "ImportTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_LoyaltyPrograms_LoyaltyProgramId",
                        column: x => x.LoyaltyProgramId,
                        principalTable: "LoyaltyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_subscriptions_subscription_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "billing_invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_billing_invoices_tenant_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "tenant_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DunningLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    AttemptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DunningLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DunningLogs_tenant_subscriptions_TenantSubscriptionId",
                        column: x => x.TenantSubscriptionId,
                        principalTable: "tenant_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_IsDeleted",
                table: "WorkTasks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_Tenant_Assigned",
                table: "WorkTasks",
                columns: new[] { "TenantId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_Tenant_Milestone",
                table: "WorkTasks",
                columns: new[] { "TenantId", "MilestoneId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_Tenant_Project",
                table: "WorkTasks",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_IsDeleted",
                table: "WorkSchedules",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_Tenant_Employee_Day",
                table: "WorkSchedules",
                columns: new[] { "TenantId", "EmployeeId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseZones_IsDeleted",
                table: "WarehouseZones",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseZones_Tenant_Code",
                table: "WarehouseZones",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseZones_Tenant_Warehouse",
                table: "WarehouseZones",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseZones_TenantId",
                table: "WarehouseZones",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseShelves_IsDeleted",
                table: "WarehouseShelves",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseShelves_Tenant_Code",
                table: "WarehouseShelves",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseShelves_Tenant_Rack",
                table: "WarehouseShelves",
                columns: new[] { "TenantId", "RackId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseShelves_TenantId",
                table: "WarehouseShelves",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_IsDeleted",
                table: "Warehouses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Tenant_Active",
                table: "Warehouses",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Tenant_Code",
                table: "Warehouses",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRacks_IsDeleted",
                table: "WarehouseRacks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRacks_Tenant_Code",
                table: "WarehouseRacks",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRacks_Tenant_Zone",
                table: "WarehouseRacks",
                columns: new[] { "TenantId", "ZoneId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRacks_TenantId",
                table: "WarehouseRacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBins_IsDeleted",
                table: "WarehouseBins",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBins_Tenant_Code",
                table: "WarehouseBins",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBins_Tenant_Shelf",
                table: "WarehouseBins",
                columns: new[] { "TenantId", "ShelfId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBins_TenantId",
                table: "WarehouseBins",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted",
                table: "Users",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId1",
                table: "Users",
                column: "TenantId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_IsDeleted",
                table: "UserRoles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_TenantId",
                table: "UserRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_User_Role",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_IsDeleted",
                table: "TimeEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_Tenant_Task",
                table: "TimeEntries",
                columns: new[] { "TenantId", "WorkTaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_Tenant_User_Start",
                table: "TimeEntries",
                columns: new[] { "TenantId", "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsDeleted",
                table: "Tenants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TaxWithholdings_IsDeleted",
                table: "TaxWithholdings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TaxWithholdings_Tenant_Invoice",
                table: "TaxWithholdings",
                columns: new[] { "TenantId", "InvoiceId" },
                filter: "\"InvoiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRecords_IsDeleted",
                table: "TaxRecords",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRecords_Tenant_Period_Type",
                table: "TaxRecords",
                columns: new[] { "TenantId", "Period", "TaxType" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRetryItems_IsDeleted",
                table: "SyncRetryItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRetryItems_Tenant_Resolved_NextRetry",
                table: "SyncRetryItems",
                columns: new[] { "TenantId", "IsResolved", "NextRetryUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_IsDeleted",
                table: "SyncLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_Platform_Success",
                table: "SyncLogs",
                columns: new[] { "PlatformCode", "IsSuccess" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_Tenant_Platform_StartedAt",
                table: "SyncLogs",
                columns: new[] { "TenantId", "PlatformCode", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsDeleted",
                table: "Suppliers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Tenant_Active",
                table: "Suppliers",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Tenant_Code",
                table: "Suppliers",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Tenant_Name",
                table: "Suppliers",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFeeds_IsDeleted",
                table: "SupplierFeeds",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierFeeds_TenantId",
                table: "SupplierFeeds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAccounts_IsDeleted",
                table: "SupplierAccounts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAccounts_Tenant_AccountCode",
                table: "SupplierAccounts",
                columns: new[] { "TenantId", "AccountCode" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAccounts_Tenant_Email",
                table: "SupplierAccounts",
                columns: new[] { "TenantId", "SupplierEmail" },
                filter: "\"SupplierEmail\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAccounts_Tenant_Supplier",
                table: "SupplierAccounts",
                columns: new[] { "TenantId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_Stores_IsDeleted",
                table: "Stores",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_TenantId",
                table: "Stores",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCredentials_IsDeleted",
                table: "StoreCredentials",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockPredictions_IsDeleted",
                table: "StockPredictions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockPredictions_Tenant_Product",
                table: "StockPredictions",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPredictions_TenantId",
                table: "StockPredictions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_IsDeleted",
                table: "StockMovements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Tenant_Date",
                table: "StockMovements",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Tenant_Product_Date",
                table: "StockMovements",
                columns: new[] { "TenantId", "ProductId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Type",
                table: "StockMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_SocialFeedConfigurations_IsDeleted",
                table: "SocialFeedConfigurations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SocialFeedConfigurations_Tenant_Platform",
                table: "SocialFeedConfigurations",
                columns: new[] { "TenantId", "Platform" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialFeedConfigurations_TenantId",
                table: "SocialFeedConfigurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementLines_IsDeleted",
                table: "SettlementLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementLines_OrderId",
                table: "SettlementLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementLines_TenantId",
                table: "SettlementLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementBatches_IsDeleted",
                table: "SettlementBatches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementBatches_PeriodStart_PeriodEnd",
                table: "SettlementBatches",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementBatches_Platform",
                table: "SettlementBatches",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ExpiresAt",
                table: "Sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IsDeleted",
                table: "Sessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Tenant_User_Active",
                table: "Sessions",
                columns: new[] { "TenantId", "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TenantId",
                table: "Sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsDeleted",
                table: "Roles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IsDeleted",
                table: "RolePermissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_Role_Permission",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_IsDeleted",
                table: "ReturnRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_PlatformReturnId",
                table: "ReturnRequests",
                column: "PlatformReturnId",
                filter: "\"PlatformReturnId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Tenant_Order",
                table: "ReturnRequests",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Tenant_Platform",
                table: "ReturnRequests",
                columns: new[] { "TenantId", "Platform" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Tenant_RequestDate",
                table: "ReturnRequests",
                columns: new[] { "TenantId", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Tenant_Status",
                table: "ReturnRequests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequestLines_IsDeleted",
                table: "ReturnRequestLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequestLines_Tenant_Product",
                table: "ReturnRequestLines",
                columns: new[] { "TenantId", "ProductId" },
                filter: "\"ProductId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequestLines_Tenant_ReturnRequest",
                table: "ReturnRequestLines",
                columns: new[] { "TenantId", "ReturnRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequestLines_TenantId",
                table: "ReturnRequestLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_BankTransactionId",
                table: "ReconciliationMatches",
                column: "BankTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_IsDeleted",
                table: "ReconciliationMatches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_SettlementBatchId",
                table: "ReconciliationMatches",
                column: "SettlementBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_Status",
                table: "ReconciliationMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_IsDeleted",
                table: "Quotations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_Tenant_Number",
                table: "Quotations",
                columns: new[] { "TenantId", "QuotationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_TenantId",
                table: "Quotations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_IsDeleted",
                table: "QuotationLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_TenantId",
                table: "QuotationLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_IsDeleted",
                table: "Projects",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tenant_Owner",
                table: "Projects",
                columns: new[] { "TenantId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tenant_Status",
                table: "Projects",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_IsDeleted",
                table: "ProjectMembers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_Project_User",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitReports_IsDeleted",
                table: "ProfitReports",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitReports_Tenant_Date",
                table: "ProfitReports",
                columns: new[] { "TenantId", "ReportDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitReports_Tenant_Platform",
                table: "ProfitReports",
                columns: new[] { "TenantId", "Platform" },
                filter: "\"Platform\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehouseStocks_IsDeleted",
                table: "ProductWarehouseStocks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehouseStocks_Tenant_Product_Warehouse",
                table: "ProductWarehouseStocks",
                columns: new[] { "TenantId", "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehouseStocks_Tenant_Warehouse",
                table: "ProductWarehouseStocks",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductWarehouseStocks_TenantId",
                table: "ProductWarehouseStocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_IsDeleted",
                table: "ProductVariants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_TenantId",
                table: "ProductVariants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSets_IsDeleted",
                table: "ProductSets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSetItems_IsDeleted",
                table: "ProductSetItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSetItems_Set_Product",
                table: "ProductSetItems",
                columns: new[] { "ProductSetId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSetItems_TenantId",
                table: "ProductSetItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                filter: "\"IsDeleted\" = false AND \"Barcode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                table: "Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Tenant_Active",
                table: "Products",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Tenant_Category",
                table: "Products",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Tenant_SKU",
                table: "Products",
                columns: new[] { "TenantId", "SKU" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPlatformMappings_IsDeleted",
                table: "ProductPlatformMappings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRecommendations_IsDeleted",
                table: "PriceRecommendations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRecommendations_Tenant_Product",
                table: "PriceRecommendations",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceRecommendations_TenantId",
                table: "PriceRecommendations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPayments_IsDeleted",
                table: "PlatformPayments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPayments_Tenant_Platform_Period",
                table: "PlatformPayments",
                columns: new[] { "TenantId", "Platform", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPayments_Tenant_Status",
                table: "PlatformPayments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPayments_TenantId",
                table: "PlatformPayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformCommissions_IsDeleted",
                table: "PlatformCommissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformCommissions_Tenant_Active",
                table: "PlatformCommissions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformCommissions_Tenant_Platform_Category",
                table: "PlatformCommissions",
                columns: new[] { "TenantId", "Platform", "CategoryName" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformCommissions_TenantId",
                table: "PlatformCommissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineStages_IsDeleted",
                table: "PipelineStages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineStages_Tenant_Pipeline_Position",
                table: "PipelineStages",
                columns: new[] { "TenantId", "PipelineId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_IsDeleted",
                table: "Pipelines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_Tenant_Default",
                table: "Pipelines",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalExpenses_ExpenseDate",
                table: "PersonalExpenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalExpenses_IsDeleted",
                table: "PersonalExpenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_IsDeleted",
                table: "Permissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_IsDeleted",
                table: "PaymentTransactions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Tenant_Order",
                table: "PaymentTransactions",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Tenant_Status",
                table: "PaymentTransactions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_TenantId",
                table: "PaymentTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExternalId",
                table: "Orders",
                column: "ExternalOrderId",
                filter: "\"ExternalOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IsDeleted",
                table: "Orders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Tenant_Customer",
                table: "Orders",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Tenant_Date",
                table: "Orders",
                columns: new[] { "TenantId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Tenant_Number",
                table: "Orders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_IsDeleted",
                table: "OrderItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductSKU",
                table: "OrderItems",
                column: "ProductSKU");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_Tenant_Order",
                table: "OrderItems",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_Tenant_Product",
                table: "OrderItems",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_TenantId",
                table: "OrderItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineQueueItems_IsDeleted",
                table: "OfflineQueueItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineQueueItems_NextAttemptAt",
                table: "OfflineQueueItems",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineQueueItems_Tenant_Status",
                table: "OfflineQueueItems",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_IsDeleted",
                table: "NotificationLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Tenant_Channel",
                table: "NotificationLogs",
                columns: new[] { "TenantId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Tenant_CreatedAt",
                table: "NotificationLogs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Tenant_Status",
                table: "NotificationLogs",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TenantId",
                table: "NotificationLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_IsDeleted",
                table: "Milestones",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_Tenant_Project",
                table: "Milestones",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_IsDeleted",
                table: "LogEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Tenant_Level",
                table: "LogEntries",
                columns: new[] { "TenantId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Tenant_Timestamp",
                table: "LogEntries",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_TenantId",
                table: "LogEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_IsDeleted",
                table: "LegalEntities",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_Tenant_Default",
                table: "LegalEntities",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_Tenant_TaxNumber",
                table: "LegalEntities",
                columns: new[] { "TenantId", "TaxNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_IsDeleted",
                table: "Leaves",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_Tenant_Employee",
                table: "Leaves",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_Tenant_Status",
                table: "Leaves",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_IsDeleted",
                table: "Leads",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Tenant_Assigned",
                table: "Leads",
                columns: new[] { "TenantId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_KontorBalances_IsDeleted",
                table: "KontorBalances",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_IsDeleted",
                table: "JournalLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_TenantId",
                table: "JournalLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryDate",
                table: "JournalEntries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_IsDeleted",
                table: "JournalEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ReferenceNumber",
                table: "JournalEntries",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_Tenant_Reference",
                table: "JournalEntries",
                columns: new[] { "TenantId", "ReferenceNumber" },
                unique: true,
                filter: "[ReferenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTemplates_IsDeleted",
                table: "InvoiceTemplates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTemplates_Tenant_Store_Default",
                table: "InvoiceTemplates",
                columns: new[] { "TenantId", "StoreId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_GibId",
                table: "Invoices",
                column: "GibInvoiceId",
                filter: "\"GibInvoiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IsDeleted",
                table: "Invoices",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Tenant_Date",
                table: "Invoices",
                columns: new[] { "TenantId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Tenant_Number",
                table: "Invoices",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId",
                table: "Invoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_IsDeleted",
                table: "InvoiceLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_Tenant_Invoice",
                table: "InvoiceLines",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_TenantId",
                table: "InvoiceLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_Expiry",
                table: "InventoryLots",
                column: "ExpiryDate",
                filter: "\"ExpiryDate\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_IsDeleted",
                table: "InventoryLots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_Tenant_Product_Lot",
                table: "InventoryLots",
                columns: new[] { "TenantId", "ProductId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_Tenant_Status",
                table: "InventoryLots",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_IsDeleted",
                table: "Incomes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_Tenant_Store",
                table: "Incomes",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_Tenant_Type",
                table: "Incomes",
                columns: new[] { "TenantId", "IncomeType" });

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_TenantId",
                table: "Incomes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GLTransactions_IsDeleted",
                table: "GLTransactions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_GLTransactions_Tenant_Account",
                table: "GLTransactions",
                columns: new[] { "TenantId", "GLAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_GLTransactions_Tenant_Order",
                table: "GLTransactions",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialGoals_IsDeleted",
                table: "FinancialGoals",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialGoals_Tenant_Achieved",
                table: "FinancialGoals",
                columns: new[] { "TenantId", "IsAchieved" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpenses_IsDeleted",
                table: "FinanceExpenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpenses_Tenant_Date",
                table: "FinanceExpenses",
                columns: new[] { "TenantId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpenses_Tenant_Status",
                table: "FinanceExpenses",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpenses_TenantId",
                table: "FinanceExpenses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedImportLogs_IsDeleted",
                table: "FeedImportLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_IsDeleted",
                table: "Expenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Tenant_PaymentStatus",
                table: "Expenses",
                columns: new[] { "TenantId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Tenant_Supplier",
                table: "Expenses",
                columns: new[] { "TenantId", "SupplierId" },
                filter: "\"SupplierId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Tenant_Type",
                table: "Expenses",
                columns: new[] { "TenantId", "ExpenseType" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TenantId",
                table: "Expenses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_erp_sync_logs_IsDeleted",
                table: "erp_sync_logs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsDeleted",
                table: "Employees",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Tenant_Department",
                table: "Employees",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Tenant_User",
                table: "Employees",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_e_invoice_send_logs_IsDeleted",
                table: "e_invoice_send_logs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_e_invoice_send_logs_TenantId",
                table: "e_invoice_send_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_e_invoice_lines_IsDeleted",
                table: "e_invoice_lines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_e_invoice_lines_TenantId",
                table: "e_invoice_lines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_e_invoice_documents_IsDeleted",
                table: "e_invoice_documents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_e_invoice_documents_TenantId",
                table: "e_invoice_documents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_einvoice_documents_tenant_deleted",
                table: "e_invoice_documents",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshipSuppliers_IsDeleted",
                table: "DropshipSuppliers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DropshipSuppliers_Tenant_Active",
                table: "DropshipSuppliers",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshipSuppliers_Tenant_Name",
                table: "DropshipSuppliers",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshipSuppliers_TenantId",
                table: "DropshipSuppliers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DropshipProducts_IsDeleted",
                table: "DropshipProducts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DropshipProducts_Tenant_ExternalId",
                table: "DropshipProducts",
                columns: new[] { "TenantId", "ExternalProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshipProducts_Tenant_LinkedProduct",
                table: "DropshipProducts",
                columns: new[] { "TenantId", "ProductId" },
                filter: "\"ProductId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DropshipProducts_Tenant_Supplier",
                table: "DropshipProducts",
                columns: new[] { "TenantId", "DropshipSupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshipProducts_TenantId",
                table: "DropshipProducts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPools_IsDeleted",
                table: "DropshippingPools",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPoolProducts_IsDeleted",
                table: "DropshippingPoolProducts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DropshipOrders_IsDeleted",
                table: "DropshipOrders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DropshipOrders_Tenant_Order",
                table: "DropshipOrders",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshipOrders_Tenant_Status",
                table: "DropshipOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshipOrders_Tenant_Supplier",
                table: "DropshipOrders",
                columns: new[] { "TenantId", "DropshipSupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshipOrders_TenantId",
                table: "DropshipOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_IsDeleted",
                table: "Documents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Tenant_Order",
                table: "Documents",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Tenant_Uploader",
                table: "Documents",
                columns: new[] { "TenantId", "UploadedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_IsDeleted",
                table: "DocumentFolders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_Tenant_Name_Parent",
                table: "DocumentFolders",
                columns: new[] { "TenantId", "Name", "ParentFolderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_Tenant_Parent",
                table: "DocumentFolders",
                columns: new[] { "TenantId", "ParentFolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_IsDeleted",
                table: "Departments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Tenant_Name",
                table: "Departments",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Tenant_Parent",
                table: "Departments",
                columns: new[] { "TenantId", "ParentDepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_IsDeleted",
                table: "Deals",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_Tenant_Assigned",
                table: "Deals",
                columns: new[] { "TenantId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_Tenant_Pipeline_Stage",
                table: "Deals",
                columns: new[] { "TenantId", "PipelineId", "StageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_Tenant_Status",
                table: "Deals",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsDeleted",
                table: "Customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxNumber",
                table: "Customers",
                column: "TaxNumber",
                filter: "\"TaxNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Tenant_Email",
                table: "Customers",
                columns: new[] { "TenantId", "Email" },
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Tenant_Phone",
                table: "Customers",
                columns: new[] { "TenantId", "Phone" },
                filter: "\"Phone\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_IsDeleted",
                table: "CustomerAccounts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_Tenant_AccountCode",
                table: "CustomerAccounts",
                columns: new[] { "TenantId", "AccountCode" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_Tenant_Customer",
                table: "CustomerAccounts",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_Tenant_Email",
                table: "CustomerAccounts",
                columns: new[] { "TenantId", "CustomerEmail" },
                filter: "\"CustomerEmail\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_IsDeleted",
                table: "CrmContacts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_Tenant_Active",
                table: "CrmContacts",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_Tenant_Customer",
                table: "CrmContacts",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_Tenant_Email",
                table: "CrmContacts",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_IsDeleted",
                table: "Counterparties",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_Tenant_VKN",
                table: "Counterparties",
                columns: new[] { "TenantId", "VKN" },
                filter: "\"VKN\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySettings_IsDeleted",
                table: "CompanySettings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySettings_TenantId",
                table: "CompanySettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_IsDeleted",
                table: "CommissionRecords",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_OrderId",
                table: "CommissionRecords",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_Platform",
                table: "CommissionRecords",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_CircuitStateLogs_IsDeleted",
                table: "CircuitStateLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CircuitStateLogs_Tenant_TransitionTime",
                table: "CircuitStateLogs",
                columns: new[] { "TenantId", "TransitionTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CircuitStateLogs_TenantId",
                table: "CircuitStateLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_IsDeleted",
                table: "ChartOfAccounts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPlatformMappings_IsDeleted",
                table: "CategoryPlatformMappings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsDeleted",
                table: "Categories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Tenant_Active",
                table: "Categories",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Tenant_Code",
                table: "Categories",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Tenant_Parent",
                table: "Categories",
                columns: new[] { "TenantId", "ParentCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowEntries_Direction",
                table: "CashFlowEntries",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowEntries_EntryDate",
                table: "CashFlowEntries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowEntries_IsDeleted",
                table: "CashFlowEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CariHesaplar_IsDeleted",
                table: "CariHesaplar",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CariHesaplar_Tenant_Email",
                table: "CariHesaplar",
                columns: new[] { "TenantId", "Email" },
                filter: "\"Email\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CariHesaplar_Tenant_TaxNumber",
                table: "CariHesaplar",
                columns: new[] { "TenantId", "TaxNumber" },
                filter: "\"TaxNumber\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CariHesaplar_TenantId",
                table: "CariHesaplar",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHareketler_IsDeleted",
                table: "CariHareketler",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CariHareketler_Tenant_Date",
                table: "CariHareketler",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CariHareketler_Tenant_Direction",
                table: "CariHareketler",
                columns: new[] { "TenantId", "Direction" });

            migrationBuilder.CreateIndex(
                name: "IX_CariHareketler_Tenant_Hesap_Date",
                table: "CariHareketler",
                columns: new[] { "TenantId", "CariHesapId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CariHareketler_TenantId",
                table: "CariHareketler",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CargoExpenses_IsDeleted",
                table: "CargoExpenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CargoExpenses_OrderId",
                table: "CargoExpenses",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_IsDeleted",
                table: "CalendarEvents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_Tenant_Creator",
                table: "CalendarEvents",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventAttendees_IsDeleted",
                table: "CalendarEventAttendees",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventAttendees_Tenant_Event_User",
                table: "CalendarEventAttendees",
                columns: new[] { "TenantId", "CalendarEventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventAttendees_TenantId",
                table: "CalendarEventAttendees",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_IsDeleted",
                table: "Brands",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_TenantId",
                table: "Brands",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BrandPlatformMappings_IsDeleted",
                table: "BrandPlatformMappings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Bitrix24Deals_IsDeleted",
                table: "Bitrix24Deals",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Bitrix24DealProductRows_IsDeleted",
                table: "Bitrix24DealProductRows",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Bitrix24DealProductRows_TenantId",
                table: "Bitrix24DealProductRows",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Bitrix24Contacts_IsDeleted",
                table: "Bitrix24Contacts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeScanLogs_Barcode",
                table: "BarcodeScanLogs",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeScanLogs_IsDeleted",
                table: "BarcodeScanLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeScanLogs_Tenant_Timestamp",
                table: "BarcodeScanLogs",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeScanLogs_TenantId",
                table: "BarcodeScanLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_IsDeleted",
                table: "BankAccounts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_Tenant_Active",
                table: "BankAccounts",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_Tenant_IBAN",
                table: "BankAccounts",
                columns: new[] { "TenantId", "IBAN" },
                unique: true,
                filter: "\"IBAN\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_CorrelationId",
                table: "ApiCallLogs",
                column: "CorrelationId",
                filter: "\"CorrelationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_IsDeleted",
                table: "ApiCallLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_Tenant_Endpoint_Success",
                table: "ApiCallLogs",
                columns: new[] { "TenantId", "Endpoint", "Success" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_Tenant_Timestamp",
                table: "ApiCallLogs",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_TenantId",
                table: "ApiCallLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_IsDeleted",
                table: "Activities",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Tenant_Contact",
                table: "Activities",
                columns: new[] { "TenantId", "CrmContactId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Tenant_OccurredAt",
                table: "Activities",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_DocumentNumber",
                table: "AccountTransactions",
                column: "DocumentNumber",
                filter: "\"DocumentNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_IsDeleted",
                table: "AccountTransactions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_Tenant_Account_Date",
                table: "AccountTransactions",
                columns: new[] { "TenantId", "AccountId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_Tenant_Date",
                table: "AccountTransactions",
                columns: new[] { "TenantId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSupplierAccounts_IsDeleted",
                table: "AccountingSupplierAccounts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingExpenseCategories_IsDeleted",
                table: "AccountingExpenseCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Tenant_Code",
                table: "AccountingExpenseCategories",
                columns: new[] { "TenantId", "Code" },
                filter: "\"Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Tenant_Parent",
                table: "AccountingExpenseCategories",
                columns: new[] { "TenantId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingDocuments_IsDeleted",
                table: "AccountingDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingDocuments_Tenant_Counterparty",
                table: "AccountingDocuments",
                columns: new[] { "TenantId", "CounterpartyId" },
                filter: "\"CounterpartyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingBankTransactions_BankAccountId",
                table: "AccountingBankTransactions",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingBankTransactions_IsDeleted",
                table: "AccountingBankTransactions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_IdempotencyKey",
                table: "AccountingBankTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogs_IsDeleted",
                table: "AccessLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogs_Tenant_AccessTime",
                table: "AccessLogs",
                columns: new[] { "TenantId", "AccessTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogs_TenantId",
                table: "AccessLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogs_UserId",
                table: "AccessLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_IsDeleted",
                table: "AccountingPeriods",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_Tenant_Year_Month",
                table: "AccountingPeriods",
                columns: new[] { "TenantId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_TenantId",
                table: "AccountingPeriods",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Tenant_Entity",
                table: "AuditLogs",
                columns: new[] { "TenantId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Tenant_Timestamp",
                table: "AuditLogs",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Tenant_User",
                table: "AuditLogs",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BaBsRecords_IsDeleted",
                table: "BaBsRecords",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BaBsRecords_Tenant_Period_Type",
                table: "BaBsRecords",
                columns: new[] { "TenantId", "Year", "Month", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_BaBsRecords_Tenant_Vkn",
                table: "BaBsRecords",
                columns: new[] { "TenantId", "CounterpartyVkn" });

            migrationBuilder.CreateIndex(
                name: "IX_BaBsRecords_TenantId",
                table: "BaBsRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupEntries_IsDeleted",
                table: "BackupEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BackupEntries_Tenant_Created",
                table: "BackupEntries",
                columns: new[] { "TenantId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_BackupEntries_TenantId",
                table: "BackupEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoices_DueDate",
                table: "billing_invoices",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoices_InvoiceNumber",
                table: "billing_invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoices_IsDeleted",
                table: "billing_invoices",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoices_Status",
                table: "billing_invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoices_SubscriptionId",
                table: "billing_invoices",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoices_TenantId",
                table: "billing_invoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlan_IsDeleted",
                table: "BudgetPlan",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlan_TenantId",
                table: "BudgetPlan",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlans_Tenant_Period",
                table: "BudgetPlan",
                columns: new[] { "TenantId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProducts_CampaignId",
                table: "CampaignProducts",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProducts_IsDeleted",
                table: "CampaignProducts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProducts_Tenant_Campaign_Product",
                table: "CampaignProducts",
                columns: new[] { "TenantId", "CampaignId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProducts_TenantId",
                table: "CampaignProducts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_IsDeleted",
                table: "Campaigns",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Tenant_Active",
                table: "Campaigns",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Tenant_DateRange",
                table: "Campaigns",
                columns: new[] { "TenantId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_TenantId",
                table: "Campaigns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_registers_IsDeleted",
                table: "cash_registers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_cash_registers_TenantId",
                table: "cash_registers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_registers_TenantId_Name",
                table: "cash_registers",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_transactions_CashRegisterId",
                table: "cash_transactions",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_transactions_IsDeleted",
                table: "cash_transactions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_cash_transactions_TenantId",
                table: "cash_transactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_transactions_TransactionDate",
                table: "cash_transactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_cash_transactions_Type",
                table: "cash_transactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CiceksepetiCategories_Tenant_Parent",
                table: "CiceksepetiCategory",
                columns: new[] { "TenantId", "ParentCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CiceksepetiCategories_Tenant_PlatformId",
                table: "CiceksepetiCategory",
                columns: new[] { "TenantId", "CiceksepetiCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CiceksepetiCategory_IsDeleted",
                table: "CiceksepetiCategory",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CiceksepetiCategory_TenantId",
                table: "CiceksepetiCategory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DunningLogs_IsDeleted",
                table: "DunningLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DunningLogs_TenantId",
                table: "DunningLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DunningLogs_TenantSubscriptionId",
                table: "DunningLogs",
                column: "TenantSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_erp_conflict_logs_IsDeleted",
                table: "erp_conflict_logs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_erp_conflict_logs_TenantId",
                table: "erp_conflict_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpConflictLogs_Entity",
                table: "erp_conflict_logs",
                columns: new[] { "EntityType", "EntityCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ErpConflictLogs_Provider",
                table: "erp_conflict_logs",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_ErpAccountMappings_IsDeleted",
                table: "ErpAccountMappings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ErpAccountMappings_Tenant_ErpCode",
                table: "ErpAccountMappings",
                columns: new[] { "TenantId", "ErpAccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErpAccountMappings_Tenant_MesTechCode",
                table: "ErpAccountMappings",
                columns: new[] { "TenantId", "MesTechAccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErpAccountMappings_TenantId",
                table: "ErpAccountMappings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_IsDeleted",
                table: "FixedAssets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_Tenant_Active",
                table: "FixedAssets",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_Tenant_Code",
                table: "FixedAssets",
                columns: new[] { "TenantId", "AssetCode" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId",
                table: "FixedAssets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedExpenses_IsActive",
                table: "FixedExpenses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FixedExpenses_IsDeleted",
                table: "FixedExpenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FixedExpenses_TenantId",
                table: "FixedExpenses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentShipments_IsDeleted",
                table: "FulfillmentShipments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentShipments_Tenant_Center_Status",
                table: "FulfillmentShipments",
                columns: new[] { "TenantId", "Center", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentShipments_Tenant_Created",
                table: "FulfillmentShipments",
                columns: new[] { "TenantId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentShipments_TenantId",
                table: "FulfillmentShipments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentShipments_Tracking",
                table: "FulfillmentShipments",
                column: "TrackingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HepsiburadaListing_IsDeleted",
                table: "HepsiburadaListing",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HepsiburadaListing_TenantId",
                table: "HepsiburadaListing",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HepsiburadaListings_Tenant_SKU",
                table: "HepsiburadaListing",
                columns: new[] { "TenantId", "HepsiburadaSKU" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportFieldMapping_ImportTemplateId",
                table: "ImportFieldMapping",
                column: "ImportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportFieldMapping_IsDeleted",
                table: "ImportFieldMapping",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ImportTemplate_IsDeleted",
                table: "ImportTemplate",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ImportTemplate_TenantId",
                table: "ImportTemplate",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KvkkAuditLogs_IsDeleted",
                table: "KvkkAuditLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_KvkkAuditLogs_Tenant_OpType",
                table: "KvkkAuditLogs",
                columns: new[] { "TenantId", "OperationType" });

            migrationBuilder.CreateIndex(
                name: "IX_KvkkAuditLogs_TenantId",
                table: "KvkkAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KvkkAuditLogs_User",
                table: "KvkkAuditLogs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Ip_Time",
                table: "LoginAttempts",
                columns: new[] { "IpAddress", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IsDeleted",
                table: "LoginAttempts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Tenant_User_Time",
                table: "LoginAttempts",
                columns: new[] { "TenantId", "Username", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_TenantId",
                table: "LoginAttempts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPrograms_IsDeleted",
                table: "LoyaltyPrograms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPrograms_Tenant_Active",
                table: "LoyaltyPrograms",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPrograms_TenantId",
                table: "LoyaltyPrograms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_IsDeleted",
                table: "LoyaltyTransactions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_LoyaltyProgramId",
                table: "LoyaltyTransactions",
                column: "LoyaltyProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_Tenant_Customer",
                table: "LoyaltyTransactions",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_Tenant_Program",
                table: "LoyaltyTransactions",
                columns: new[] { "TenantId", "LoyaltyProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_TenantId",
                table: "LoyaltyTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_IsDeleted",
                table: "NotificationSettings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_Tenant_User_Channel",
                table: "NotificationSettings",
                columns: new[] { "TenantId", "UserId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_TenantId",
                table: "NotificationSettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_UserId",
                table: "NotificationSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_IsDeleted",
                table: "NotificationTemplates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Tenant_Active",
                table: "NotificationTemplates",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Tenant_Name_Channel",
                table: "NotificationTemplates",
                columns: new[] { "TenantId", "TemplateName", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_TenantId",
                table: "NotificationTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_progress_IsCompleted",
                table: "onboarding_progress",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_progress_IsDeleted",
                table: "onboarding_progress",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_progress_TenantId",
                table: "onboarding_progress",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyRecords_IsDeleted",
                table: "PenaltyRecords",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyRecords_Tenant_Date",
                table: "PenaltyRecords",
                columns: new[] { "TenantId", "PenaltyDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyRecords_Tenant_PaymentStatus",
                table: "PenaltyRecords",
                columns: new[] { "TenantId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyRecords_Tenant_Source",
                table: "PenaltyRecords",
                columns: new[] { "TenantId", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyRecords_TenantId",
                table: "PenaltyRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataRetentionPolicies_IsDeleted",
                table: "PersonalDataRetentionPolicies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataRetentionPolicies_TenantId",
                table: "PersonalDataRetentionPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionPolicies_EntityType",
                table: "PersonalDataRetentionPolicies",
                column: "EntityTypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformMessages_IsDeleted",
                table: "PlatformMessages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformMessages_Tenant_Order",
                table: "PlatformMessages",
                columns: new[] { "TenantId", "OrderId" },
                filter: "\"OrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformMessages_Tenant_Platform_ExtId",
                table: "PlatformMessages",
                columns: new[] { "TenantId", "Platform", "ExternalMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformMessages_Tenant_Status",
                table: "PlatformMessages",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformMessages_TenantId",
                table: "PlatformMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_IsDeleted",
                table: "PriceHistories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_ProductId",
                table: "PriceHistories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_TenantId",
                table: "PriceHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_Tenant_Platform",
                table: "PriceHistories",
                columns: new[] { "TenantId", "Platform" },
                filter: "\"Platform\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_Tenant_Product_Date",
                table: "PriceHistories",
                columns: new[] { "TenantId", "ProductId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedDomainEvents_EventId_Handler",
                table: "ProcessedDomainEvents",
                columns: new[] { "EventId", "HandlerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedDomainEvents_TenantId",
                table: "ProcessedDomainEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossEntries_Tenant_Period",
                table: "ProfitLossEntry",
                columns: new[] { "TenantId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossEntry_IsDeleted",
                table: "ProfitLossEntry",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossEntry_TenantId",
                table: "ProfitLossEntry",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_IsActive",
                table: "RecurringExpense",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_IsDeleted",
                table: "RecurringExpense",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_NextDueDate",
                table: "RecurringExpense",
                column: "NextDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_TenantId",
                table: "RecurringExpense",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_IsDeleted",
                table: "refresh_tokens",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TenantId",
                table: "refresh_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_active",
                table: "refresh_tokens",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRecords_IsDeleted",
                table: "SalaryRecords",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRecords_Tenant_PaymentStatus",
                table: "SalaryRecords",
                columns: new[] { "TenantId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRecords_Tenant_Period",
                table: "SalaryRecords",
                columns: new[] { "TenantId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRecords_TenantId",
                table: "SalaryRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_IsDeleted",
                table: "SavedReports",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_Tenant_Creator",
                table: "SavedReports",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_Tenant_Type",
                table: "SavedReports",
                columns: new[] { "TenantId", "ReportType" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_TenantId",
                table: "SavedReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCosts_IsDeleted",
                table: "ShipmentCosts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCosts_Tenant_Order",
                table: "ShipmentCosts",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCosts_TenantId",
                table: "ShipmentCosts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCosts_Tracking",
                table: "ShipmentCosts",
                column: "TrackingNumber",
                filter: "\"TrackingNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlertRules_IsDeleted",
                table: "StockAlertRules",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlertRules_ProductId",
                table: "StockAlertRules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlertRules_Tenant_Product_Warehouse",
                table: "StockAlertRules",
                columns: new[] { "TenantId", "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAlertRules_TenantId",
                table: "StockAlertRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_IsDeleted",
                table: "StockAlerts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_ProductId",
                table: "StockAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_Tenant_Date",
                table: "StockAlerts",
                columns: new[] { "TenantId", "AlertDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_Tenant_Level",
                table: "StockAlerts",
                columns: new[] { "TenantId", "AlertLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_Tenant_Product_Resolved",
                table: "StockAlerts",
                columns: new[] { "TenantId", "ProductId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_TenantId",
                table: "StockAlerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_IsDeleted",
                table: "StockLots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_ProductId",
                table: "StockLots",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_Tenant_LotNumber",
                table: "StockLots",
                columns: new[] { "TenantId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_Tenant_Product",
                table: "StockLots",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_Tenant_Received",
                table: "StockLots",
                columns: new[] { "TenantId", "ReceivedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_TenantId",
                table: "StockLots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_WarehouseId",
                table: "StockLots",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPlacements_IsDeleted",
                table: "StockPlacements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockPlacements_Location_Product",
                table: "StockPlacements",
                columns: new[] { "TenantId", "WarehouseId", "ShelfId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockPlacements_ProductId",
                table: "StockPlacements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPlacements_Tenant_Product",
                table: "StockPlacements",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPlacements_TenantId",
                table: "StockPlacements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPlacements_WarehouseId",
                table: "StockPlacements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_IsActive",
                table: "subscription_plans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_IsDeleted",
                table: "subscription_plans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_Name",
                table: "subscription_plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_TenantId",
                table: "subscription_plans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_IsDeleted",
                table: "tenant_subscriptions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_NextBillingDate",
                table: "tenant_subscriptions",
                column: "NextBillingDate");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_PlanId",
                table: "tenant_subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_Status",
                table: "tenant_subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_TenantId",
                table: "tenant_subscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_IsDeleted",
                table: "UserConsents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_Tenant_Type_Accepted",
                table: "UserConsents",
                columns: new[] { "TenantId", "ConsentType", "IsAccepted" });

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_Tenant_User_Type",
                table: "UserConsents",
                columns: new[] { "TenantId", "UserId", "ConsentType" });

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_TenantId",
                table: "UserConsents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_IsDeleted",
                table: "UserNotifications",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_Tenant_User_Read",
                table: "UserNotifications",
                columns: new[] { "TenantId", "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_TenantId",
                table: "UserNotifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeadLetters_IsDeleted",
                table: "WebhookDeadLetters",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeadLetters_NextRetry_Pending",
                table: "WebhookDeadLetters",
                column: "NextRetryAt",
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeadLetters_Tenant_Status",
                table: "WebhookDeadLetters",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeadLetters_TenantId",
                table: "WebhookDeadLetters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_Tenant_Platform_ReceivedAt",
                table: "WebhookLogs",
                columns: new[] { "TenantId", "Platform", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_TenantId",
                table: "WebhookLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_Valid_Retry",
                table: "WebhookLogs",
                columns: new[] { "IsValid", "RetryCount" });

            migrationBuilder.AddForeignKey(
                name: "FK_AccountTransactions_CustomerAccounts_AccountId",
                table: "AccountTransactions",
                column: "AccountId",
                principalTable: "CustomerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountTransactions_SupplierAccounts_AccountId",
                table: "AccountTransactions",
                column: "AccountId",
                principalTable: "SupplierAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerAccounts_Customers_CustomerId",
                table: "CustomerAccounts",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Employees_EmployeeId",
                table: "Leaves",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformPayments_Stores_StoreId",
                table: "PlatformPayments",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Customers_CustomerId",
                table: "Quotations",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequestLines_Products_ProductId",
                table: "ReturnRequestLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequests_Orders_OrderId",
                table: "ReturnRequests",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequests_Stores_StoreId",
                table: "ReturnRequests",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierAccounts_Suppliers_SupplierId",
                table: "SupplierAccounts",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountTransactions_CustomerAccounts_AccountId",
                table: "AccountTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountTransactions_SupplierAccounts_AccountId",
                table: "AccountTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerAccounts_Customers_CustomerId",
                table: "CustomerAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Leaves_Employees_EmployeeId",
                table: "Leaves");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformPayments_Stores_StoreId",
                table: "PlatformPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Customers_CustomerId",
                table: "Quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequestLines_Products_ProductId",
                table: "ReturnRequestLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequests_Orders_OrderId",
                table: "ReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequests_Stores_StoreId",
                table: "ReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierAccounts_Suppliers_SupplierId",
                table: "SupplierAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BaBsRecords");

            migrationBuilder.DropTable(
                name: "BackupEntries");

            migrationBuilder.DropTable(
                name: "billing_invoices");

            migrationBuilder.DropTable(
                name: "BudgetPlan");

            migrationBuilder.DropTable(
                name: "CampaignProducts");

            migrationBuilder.DropTable(
                name: "cash_transactions");

            migrationBuilder.DropTable(
                name: "CiceksepetiCategory");

            migrationBuilder.DropTable(
                name: "DunningLogs");

            migrationBuilder.DropTable(
                name: "erp_conflict_logs");

            migrationBuilder.DropTable(
                name: "ErpAccountMappings");

            migrationBuilder.DropTable(
                name: "FixedAssets");

            migrationBuilder.DropTable(
                name: "FixedExpenses");

            migrationBuilder.DropTable(
                name: "FulfillmentShipments");

            migrationBuilder.DropTable(
                name: "HepsiburadaListing");

            migrationBuilder.DropTable(
                name: "ImportFieldMapping");

            migrationBuilder.DropTable(
                name: "KvkkAuditLogs");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "onboarding_progress");

            migrationBuilder.DropTable(
                name: "PenaltyRecords");

            migrationBuilder.DropTable(
                name: "PersonalDataRetentionPolicies");

            migrationBuilder.DropTable(
                name: "PlatformMessages");

            migrationBuilder.DropTable(
                name: "PriceHistories");

            migrationBuilder.DropTable(
                name: "ProcessedDomainEvents");

            migrationBuilder.DropTable(
                name: "ProfitLossEntry");

            migrationBuilder.DropTable(
                name: "RecurringExpense");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "SalaryRecords");

            migrationBuilder.DropTable(
                name: "SavedReports");

            migrationBuilder.DropTable(
                name: "ShipmentCosts");

            migrationBuilder.DropTable(
                name: "StockAlertRules");

            migrationBuilder.DropTable(
                name: "StockAlerts");

            migrationBuilder.DropTable(
                name: "StockLots");

            migrationBuilder.DropTable(
                name: "StockPlacements");

            migrationBuilder.DropTable(
                name: "UserConsents");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "WebhookDeadLetters");

            migrationBuilder.DropTable(
                name: "WebhookLogs");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "cash_registers");

            migrationBuilder.DropTable(
                name: "tenant_subscriptions");

            migrationBuilder.DropTable(
                name: "ImportTemplate");

            migrationBuilder.DropTable(
                name: "LoyaltyPrograms");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_IsDeleted",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_Tenant_Assigned",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_Tenant_Milestone",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_Tenant_Project",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_IsDeleted",
                table: "WorkSchedules");

            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_Tenant_Employee_Day",
                table: "WorkSchedules");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseZones_IsDeleted",
                table: "WarehouseZones");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseZones_Tenant_Code",
                table: "WarehouseZones");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseZones_Tenant_Warehouse",
                table: "WarehouseZones");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseZones_TenantId",
                table: "WarehouseZones");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseShelves_IsDeleted",
                table: "WarehouseShelves");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseShelves_Tenant_Code",
                table: "WarehouseShelves");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseShelves_Tenant_Rack",
                table: "WarehouseShelves");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseShelves_TenantId",
                table: "WarehouseShelves");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_IsDeleted",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Tenant_Active",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Tenant_Code",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseRacks_IsDeleted",
                table: "WarehouseRacks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseRacks_Tenant_Code",
                table: "WarehouseRacks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseRacks_Tenant_Zone",
                table: "WarehouseRacks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseRacks_TenantId",
                table: "WarehouseRacks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseBins_IsDeleted",
                table: "WarehouseBins");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseBins_Tenant_Code",
                table: "WarehouseBins");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseBins_Tenant_Shelf",
                table: "WarehouseBins");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseBins_TenantId",
                table: "WarehouseBins");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsDeleted",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_IsDeleted",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_TenantId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_User_Role",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_IsDeleted",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_Tenant_Task",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_Tenant_User_Start",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_IsDeleted",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_TaxWithholdings_IsDeleted",
                table: "TaxWithholdings");

            migrationBuilder.DropIndex(
                name: "IX_TaxWithholdings_Tenant_Invoice",
                table: "TaxWithholdings");

            migrationBuilder.DropIndex(
                name: "IX_TaxRecords_IsDeleted",
                table: "TaxRecords");

            migrationBuilder.DropIndex(
                name: "IX_TaxRecords_Tenant_Period_Type",
                table: "TaxRecords");

            migrationBuilder.DropIndex(
                name: "IX_SyncRetryItems_IsDeleted",
                table: "SyncRetryItems");

            migrationBuilder.DropIndex(
                name: "IX_SyncRetryItems_Tenant_Resolved_NextRetry",
                table: "SyncRetryItems");

            migrationBuilder.DropIndex(
                name: "IX_SyncLogs_IsDeleted",
                table: "SyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_SyncLogs_Platform_Success",
                table: "SyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_SyncLogs_Tenant_Platform_StartedAt",
                table: "SyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_IsDeleted",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Tenant_Active",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Tenant_Code",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Tenant_Name",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_SupplierFeeds_IsDeleted",
                table: "SupplierFeeds");

            migrationBuilder.DropIndex(
                name: "IX_SupplierFeeds_TenantId",
                table: "SupplierFeeds");

            migrationBuilder.DropIndex(
                name: "IX_SupplierAccounts_IsDeleted",
                table: "SupplierAccounts");

            migrationBuilder.DropIndex(
                name: "IX_SupplierAccounts_Tenant_AccountCode",
                table: "SupplierAccounts");

            migrationBuilder.DropIndex(
                name: "IX_SupplierAccounts_Tenant_Email",
                table: "SupplierAccounts");

            migrationBuilder.DropIndex(
                name: "IX_SupplierAccounts_Tenant_Supplier",
                table: "SupplierAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Stores_IsDeleted",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Stores_TenantId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_StoreCredentials_IsDeleted",
                table: "StoreCredentials");

            migrationBuilder.DropIndex(
                name: "IX_StockPredictions_IsDeleted",
                table: "StockPredictions");

            migrationBuilder.DropIndex(
                name: "IX_StockPredictions_Tenant_Product",
                table: "StockPredictions");

            migrationBuilder.DropIndex(
                name: "IX_StockPredictions_TenantId",
                table: "StockPredictions");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_IsDeleted",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_Tenant_Date",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_Tenant_Product_Date",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_Type",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_SocialFeedConfigurations_IsDeleted",
                table: "SocialFeedConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_SocialFeedConfigurations_Tenant_Platform",
                table: "SocialFeedConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_SocialFeedConfigurations_TenantId",
                table: "SocialFeedConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_SettlementLines_IsDeleted",
                table: "SettlementLines");

            migrationBuilder.DropIndex(
                name: "IX_SettlementLines_OrderId",
                table: "SettlementLines");

            migrationBuilder.DropIndex(
                name: "IX_SettlementLines_TenantId",
                table: "SettlementLines");

            migrationBuilder.DropIndex(
                name: "IX_SettlementBatches_IsDeleted",
                table: "SettlementBatches");

            migrationBuilder.DropIndex(
                name: "IX_SettlementBatches_PeriodStart_PeriodEnd",
                table: "SettlementBatches");

            migrationBuilder.DropIndex(
                name: "IX_SettlementBatches_Platform",
                table: "SettlementBatches");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ExpiresAt",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_IsDeleted",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_Tenant_User_Active",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_TenantId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Roles_IsDeleted",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_IsDeleted",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_Role_Permission",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_IsDeleted",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_PlatformReturnId",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_Tenant_Order",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_Tenant_Platform",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_Tenant_RequestDate",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_Tenant_Status",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequestLines_IsDeleted",
                table: "ReturnRequestLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequestLines_Tenant_Product",
                table: "ReturnRequestLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequestLines_Tenant_ReturnRequest",
                table: "ReturnRequestLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequestLines_TenantId",
                table: "ReturnRequestLines");

            migrationBuilder.DropIndex(
                name: "IX_ReconciliationMatches_BankTransactionId",
                table: "ReconciliationMatches");

            migrationBuilder.DropIndex(
                name: "IX_ReconciliationMatches_IsDeleted",
                table: "ReconciliationMatches");

            migrationBuilder.DropIndex(
                name: "IX_ReconciliationMatches_SettlementBatchId",
                table: "ReconciliationMatches");

            migrationBuilder.DropIndex(
                name: "IX_ReconciliationMatches_Status",
                table: "ReconciliationMatches");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_IsDeleted",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_Tenant_Number",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_TenantId",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_QuotationLines_IsDeleted",
                table: "QuotationLines");

            migrationBuilder.DropIndex(
                name: "IX_QuotationLines_TenantId",
                table: "QuotationLines");

            migrationBuilder.DropIndex(
                name: "IX_Projects_IsDeleted",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Tenant_Owner",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Tenant_Status",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_IsDeleted",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_Project_User",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_ProfitReports_IsDeleted",
                table: "ProfitReports");

            migrationBuilder.DropIndex(
                name: "IX_ProfitReports_Tenant_Date",
                table: "ProfitReports");

            migrationBuilder.DropIndex(
                name: "IX_ProfitReports_Tenant_Platform",
                table: "ProfitReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductWarehouseStocks_IsDeleted",
                table: "ProductWarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_ProductWarehouseStocks_Tenant_Product_Warehouse",
                table: "ProductWarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_ProductWarehouseStocks_Tenant_Warehouse",
                table: "ProductWarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_ProductWarehouseStocks_TenantId",
                table: "ProductWarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_IsDeleted",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_TenantId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductSets_IsDeleted",
                table: "ProductSets");

            migrationBuilder.DropIndex(
                name: "IX_ProductSetItems_IsDeleted",
                table: "ProductSetItems");

            migrationBuilder.DropIndex(
                name: "IX_ProductSetItems_Set_Product",
                table: "ProductSetItems");

            migrationBuilder.DropIndex(
                name: "IX_ProductSetItems_TenantId",
                table: "ProductSetItems");

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Tenant_Active",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Tenant_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Tenant_SKU",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductPlatformMappings_IsDeleted",
                table: "ProductPlatformMappings");

            migrationBuilder.DropIndex(
                name: "IX_PriceRecommendations_IsDeleted",
                table: "PriceRecommendations");

            migrationBuilder.DropIndex(
                name: "IX_PriceRecommendations_Tenant_Product",
                table: "PriceRecommendations");

            migrationBuilder.DropIndex(
                name: "IX_PriceRecommendations_TenantId",
                table: "PriceRecommendations");

            migrationBuilder.DropIndex(
                name: "IX_PlatformPayments_IsDeleted",
                table: "PlatformPayments");

            migrationBuilder.DropIndex(
                name: "IX_PlatformPayments_Tenant_Platform_Period",
                table: "PlatformPayments");

            migrationBuilder.DropIndex(
                name: "IX_PlatformPayments_Tenant_Status",
                table: "PlatformPayments");

            migrationBuilder.DropIndex(
                name: "IX_PlatformPayments_TenantId",
                table: "PlatformPayments");

            migrationBuilder.DropIndex(
                name: "IX_PlatformCommissions_IsDeleted",
                table: "PlatformCommissions");

            migrationBuilder.DropIndex(
                name: "IX_PlatformCommissions_Tenant_Active",
                table: "PlatformCommissions");

            migrationBuilder.DropIndex(
                name: "IX_PlatformCommissions_Tenant_Platform_Category",
                table: "PlatformCommissions");

            migrationBuilder.DropIndex(
                name: "IX_PlatformCommissions_TenantId",
                table: "PlatformCommissions");

            migrationBuilder.DropIndex(
                name: "IX_PipelineStages_IsDeleted",
                table: "PipelineStages");

            migrationBuilder.DropIndex(
                name: "IX_PipelineStages_Tenant_Pipeline_Position",
                table: "PipelineStages");

            migrationBuilder.DropIndex(
                name: "IX_Pipelines_IsDeleted",
                table: "Pipelines");

            migrationBuilder.DropIndex(
                name: "IX_Pipelines_Tenant_Default",
                table: "Pipelines");

            migrationBuilder.DropIndex(
                name: "IX_PersonalExpenses_ExpenseDate",
                table: "PersonalExpenses");

            migrationBuilder.DropIndex(
                name: "IX_PersonalExpenses_IsDeleted",
                table: "PersonalExpenses");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_IsDeleted",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Name",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_IsDeleted",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Tenant_Order",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Tenant_Status",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_TenantId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ExternalId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_IsDeleted",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Tenant_Customer",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Tenant_Date",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Tenant_Number",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_IsDeleted",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductSKU",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_Tenant_Order",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_Tenant_Product",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_TenantId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OfflineQueueItems_IsDeleted",
                table: "OfflineQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_OfflineQueueItems_NextAttemptAt",
                table: "OfflineQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_OfflineQueueItems_Tenant_Status",
                table: "OfflineQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_IsDeleted",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_Tenant_Channel",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_Tenant_CreatedAt",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_Tenant_Status",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_TenantId",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_IsDeleted",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_Tenant_Project",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_LogEntries_IsDeleted",
                table: "LogEntries");

            migrationBuilder.DropIndex(
                name: "IX_LogEntries_Tenant_Level",
                table: "LogEntries");

            migrationBuilder.DropIndex(
                name: "IX_LogEntries_Tenant_Timestamp",
                table: "LogEntries");

            migrationBuilder.DropIndex(
                name: "IX_LogEntries_TenantId",
                table: "LogEntries");

            migrationBuilder.DropIndex(
                name: "IX_LegalEntities_IsDeleted",
                table: "LegalEntities");

            migrationBuilder.DropIndex(
                name: "IX_LegalEntities_Tenant_Default",
                table: "LegalEntities");

            migrationBuilder.DropIndex(
                name: "IX_LegalEntities_Tenant_TaxNumber",
                table: "LegalEntities");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_IsDeleted",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_Tenant_Employee",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_Tenant_Status",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Leads_IsDeleted",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_Tenant_Assigned",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_KontorBalances_IsDeleted",
                table: "KontorBalances");

            migrationBuilder.DropIndex(
                name: "IX_JournalLines_IsDeleted",
                table: "JournalLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalLines_TenantId",
                table: "JournalLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_EntryDate",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_IsDeleted",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_ReferenceNumber",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_Tenant_Reference",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceTemplates_IsDeleted",
                table: "InvoiceTemplates");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceTemplates_Tenant_Store_Default",
                table: "InvoiceTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_GibId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_IsDeleted",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Tenant_Date",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Tenant_Number",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLines_IsDeleted",
                table: "InvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLines_Tenant_Invoice",
                table: "InvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLines_TenantId",
                table: "InvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLots_Expiry",
                table: "InventoryLots");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLots_IsDeleted",
                table: "InventoryLots");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLots_Tenant_Product_Lot",
                table: "InventoryLots");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLots_Tenant_Status",
                table: "InventoryLots");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_IsDeleted",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_Tenant_Store",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_Tenant_Type",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_TenantId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_GLTransactions_IsDeleted",
                table: "GLTransactions");

            migrationBuilder.DropIndex(
                name: "IX_GLTransactions_Tenant_Account",
                table: "GLTransactions");

            migrationBuilder.DropIndex(
                name: "IX_GLTransactions_Tenant_Order",
                table: "GLTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialGoals_IsDeleted",
                table: "FinancialGoals");

            migrationBuilder.DropIndex(
                name: "IX_FinancialGoals_Tenant_Achieved",
                table: "FinancialGoals");

            migrationBuilder.DropIndex(
                name: "IX_FinanceExpenses_IsDeleted",
                table: "FinanceExpenses");

            migrationBuilder.DropIndex(
                name: "IX_FinanceExpenses_Tenant_Date",
                table: "FinanceExpenses");

            migrationBuilder.DropIndex(
                name: "IX_FinanceExpenses_Tenant_Status",
                table: "FinanceExpenses");

            migrationBuilder.DropIndex(
                name: "IX_FinanceExpenses_TenantId",
                table: "FinanceExpenses");

            migrationBuilder.DropIndex(
                name: "IX_FeedImportLogs_IsDeleted",
                table: "FeedImportLogs");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_IsDeleted",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_Tenant_PaymentStatus",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_Tenant_Supplier",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_Tenant_Type",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_TenantId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_erp_sync_logs_IsDeleted",
                table: "erp_sync_logs");

            migrationBuilder.DropIndex(
                name: "IX_Employees_IsDeleted",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Tenant_Department",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Tenant_User",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_e_invoice_send_logs_IsDeleted",
                table: "e_invoice_send_logs");

            migrationBuilder.DropIndex(
                name: "IX_e_invoice_send_logs_TenantId",
                table: "e_invoice_send_logs");

            migrationBuilder.DropIndex(
                name: "IX_e_invoice_lines_IsDeleted",
                table: "e_invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_e_invoice_lines_TenantId",
                table: "e_invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_e_invoice_documents_IsDeleted",
                table: "e_invoice_documents");

            migrationBuilder.DropIndex(
                name: "IX_e_invoice_documents_TenantId",
                table: "e_invoice_documents");

            migrationBuilder.DropIndex(
                name: "ix_einvoice_documents_tenant_deleted",
                table: "e_invoice_documents");

            migrationBuilder.DropIndex(
                name: "IX_DropshipSuppliers_IsDeleted",
                table: "DropshipSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_DropshipSuppliers_Tenant_Active",
                table: "DropshipSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_DropshipSuppliers_Tenant_Name",
                table: "DropshipSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_DropshipSuppliers_TenantId",
                table: "DropshipSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_DropshipProducts_IsDeleted",
                table: "DropshipProducts");

            migrationBuilder.DropIndex(
                name: "IX_DropshipProducts_Tenant_ExternalId",
                table: "DropshipProducts");

            migrationBuilder.DropIndex(
                name: "IX_DropshipProducts_Tenant_LinkedProduct",
                table: "DropshipProducts");

            migrationBuilder.DropIndex(
                name: "IX_DropshipProducts_Tenant_Supplier",
                table: "DropshipProducts");

            migrationBuilder.DropIndex(
                name: "IX_DropshipProducts_TenantId",
                table: "DropshipProducts");

            migrationBuilder.DropIndex(
                name: "IX_DropshippingPools_IsDeleted",
                table: "DropshippingPools");

            migrationBuilder.DropIndex(
                name: "IX_DropshippingPoolProducts_IsDeleted",
                table: "DropshippingPoolProducts");

            migrationBuilder.DropIndex(
                name: "IX_DropshipOrders_IsDeleted",
                table: "DropshipOrders");

            migrationBuilder.DropIndex(
                name: "IX_DropshipOrders_Tenant_Order",
                table: "DropshipOrders");

            migrationBuilder.DropIndex(
                name: "IX_DropshipOrders_Tenant_Status",
                table: "DropshipOrders");

            migrationBuilder.DropIndex(
                name: "IX_DropshipOrders_Tenant_Supplier",
                table: "DropshipOrders");

            migrationBuilder.DropIndex(
                name: "IX_DropshipOrders_TenantId",
                table: "DropshipOrders");

            migrationBuilder.DropIndex(
                name: "IX_Documents_IsDeleted",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Tenant_Order",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Tenant_Uploader",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentFolders_IsDeleted",
                table: "DocumentFolders");

            migrationBuilder.DropIndex(
                name: "IX_DocumentFolders_Tenant_Name_Parent",
                table: "DocumentFolders");

            migrationBuilder.DropIndex(
                name: "IX_DocumentFolders_Tenant_Parent",
                table: "DocumentFolders");

            migrationBuilder.DropIndex(
                name: "IX_Departments_IsDeleted",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Tenant_Name",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Tenant_Parent",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Deals_IsDeleted",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_Tenant_Assigned",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_Tenant_Pipeline_Stage",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_Tenant_Status",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Customers_IsDeleted",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TaxNumber",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Tenant_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Tenant_Phone",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAccounts_IsDeleted",
                table: "CustomerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAccounts_Tenant_AccountCode",
                table: "CustomerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAccounts_Tenant_Customer",
                table: "CustomerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAccounts_Tenant_Email",
                table: "CustomerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CrmContacts_IsDeleted",
                table: "CrmContacts");

            migrationBuilder.DropIndex(
                name: "IX_CrmContacts_Tenant_Active",
                table: "CrmContacts");

            migrationBuilder.DropIndex(
                name: "IX_CrmContacts_Tenant_Customer",
                table: "CrmContacts");

            migrationBuilder.DropIndex(
                name: "IX_CrmContacts_Tenant_Email",
                table: "CrmContacts");

            migrationBuilder.DropIndex(
                name: "IX_Counterparties_IsDeleted",
                table: "Counterparties");

            migrationBuilder.DropIndex(
                name: "IX_Counterparties_Tenant_VKN",
                table: "Counterparties");

            migrationBuilder.DropIndex(
                name: "IX_CompanySettings_IsDeleted",
                table: "CompanySettings");

            migrationBuilder.DropIndex(
                name: "IX_CompanySettings_TenantId",
                table: "CompanySettings");

            migrationBuilder.DropIndex(
                name: "IX_CommissionRecords_IsDeleted",
                table: "CommissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_CommissionRecords_OrderId",
                table: "CommissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_CommissionRecords_Platform",
                table: "CommissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_CircuitStateLogs_IsDeleted",
                table: "CircuitStateLogs");

            migrationBuilder.DropIndex(
                name: "IX_CircuitStateLogs_Tenant_TransitionTime",
                table: "CircuitStateLogs");

            migrationBuilder.DropIndex(
                name: "IX_CircuitStateLogs_TenantId",
                table: "CircuitStateLogs");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccounts_IsDeleted",
                table: "ChartOfAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CategoryPlatformMappings_IsDeleted",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsDeleted",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Tenant_Active",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Tenant_Code",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Tenant_Parent",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_CashFlowEntries_Direction",
                table: "CashFlowEntries");

            migrationBuilder.DropIndex(
                name: "IX_CashFlowEntries_EntryDate",
                table: "CashFlowEntries");

            migrationBuilder.DropIndex(
                name: "IX_CashFlowEntries_IsDeleted",
                table: "CashFlowEntries");

            migrationBuilder.DropIndex(
                name: "IX_CariHesaplar_IsDeleted",
                table: "CariHesaplar");

            migrationBuilder.DropIndex(
                name: "IX_CariHesaplar_Tenant_Email",
                table: "CariHesaplar");

            migrationBuilder.DropIndex(
                name: "IX_CariHesaplar_Tenant_TaxNumber",
                table: "CariHesaplar");

            migrationBuilder.DropIndex(
                name: "IX_CariHesaplar_TenantId",
                table: "CariHesaplar");

            migrationBuilder.DropIndex(
                name: "IX_CariHareketler_IsDeleted",
                table: "CariHareketler");

            migrationBuilder.DropIndex(
                name: "IX_CariHareketler_Tenant_Date",
                table: "CariHareketler");

            migrationBuilder.DropIndex(
                name: "IX_CariHareketler_Tenant_Direction",
                table: "CariHareketler");

            migrationBuilder.DropIndex(
                name: "IX_CariHareketler_Tenant_Hesap_Date",
                table: "CariHareketler");

            migrationBuilder.DropIndex(
                name: "IX_CariHareketler_TenantId",
                table: "CariHareketler");

            migrationBuilder.DropIndex(
                name: "IX_CargoExpenses_IsDeleted",
                table: "CargoExpenses");

            migrationBuilder.DropIndex(
                name: "IX_CargoExpenses_OrderId",
                table: "CargoExpenses");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_IsDeleted",
                table: "CalendarEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_Tenant_Creator",
                table: "CalendarEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEventAttendees_IsDeleted",
                table: "CalendarEventAttendees");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEventAttendees_Tenant_Event_User",
                table: "CalendarEventAttendees");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEventAttendees_TenantId",
                table: "CalendarEventAttendees");

            migrationBuilder.DropIndex(
                name: "IX_Brands_IsDeleted",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_TenantId",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_BrandPlatformMappings_IsDeleted",
                table: "BrandPlatformMappings");

            migrationBuilder.DropIndex(
                name: "IX_Bitrix24Deals_IsDeleted",
                table: "Bitrix24Deals");

            migrationBuilder.DropIndex(
                name: "IX_Bitrix24DealProductRows_IsDeleted",
                table: "Bitrix24DealProductRows");

            migrationBuilder.DropIndex(
                name: "IX_Bitrix24DealProductRows_TenantId",
                table: "Bitrix24DealProductRows");

            migrationBuilder.DropIndex(
                name: "IX_Bitrix24Contacts_IsDeleted",
                table: "Bitrix24Contacts");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeScanLogs_Barcode",
                table: "BarcodeScanLogs");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeScanLogs_IsDeleted",
                table: "BarcodeScanLogs");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeScanLogs_Tenant_Timestamp",
                table: "BarcodeScanLogs");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeScanLogs_TenantId",
                table: "BarcodeScanLogs");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_IsDeleted",
                table: "BankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_Tenant_Active",
                table: "BankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_Tenant_IBAN",
                table: "BankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ApiCallLogs_CorrelationId",
                table: "ApiCallLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApiCallLogs_IsDeleted",
                table: "ApiCallLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApiCallLogs_Tenant_Endpoint_Success",
                table: "ApiCallLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApiCallLogs_Tenant_Timestamp",
                table: "ApiCallLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApiCallLogs_TenantId",
                table: "ApiCallLogs");

            migrationBuilder.DropIndex(
                name: "IX_Activities_IsDeleted",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_Tenant_Contact",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_Tenant_OccurredAt",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_AccountTransactions_DocumentNumber",
                table: "AccountTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AccountTransactions_IsDeleted",
                table: "AccountTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AccountTransactions_Tenant_Account_Date",
                table: "AccountTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AccountTransactions_Tenant_Date",
                table: "AccountTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AccountingSupplierAccounts_IsDeleted",
                table: "AccountingSupplierAccounts");

            migrationBuilder.DropIndex(
                name: "IX_AccountingExpenseCategories_IsDeleted",
                table: "AccountingExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_Tenant_Code",
                table: "AccountingExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_Tenant_Parent",
                table: "AccountingExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_AccountingDocuments_IsDeleted",
                table: "AccountingDocuments");

            migrationBuilder.DropIndex(
                name: "IX_AccountingDocuments_Tenant_Counterparty",
                table: "AccountingDocuments");

            migrationBuilder.DropIndex(
                name: "IX_AccountingBankTransactions_BankAccountId",
                table: "AccountingBankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AccountingBankTransactions_IsDeleted",
                table: "AccountingBankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_IdempotencyKey",
                table: "AccountingBankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AccessLogs_IsDeleted",
                table: "AccessLogs");

            migrationBuilder.DropIndex(
                name: "IX_AccessLogs_Tenant_AccessTime",
                table: "AccessLogs");

            migrationBuilder.DropIndex(
                name: "IX_AccessLogs_TenantId",
                table: "AccessLogs");

            migrationBuilder.DropIndex(
                name: "IX_AccessLogs_UserId",
                table: "AccessLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WarehouseZones");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WarehouseShelves");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WarehouseRacks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WarehouseBins");

            migrationBuilder.DropColumn(
                name: "IsMfaEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaEnabledAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotpSecret",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "PenaltyAmount",
                table: "TaxRecords");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "TaxRecords");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "TaxRecords");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ProductSKU",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "QuotationLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductSetItems");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RecipientPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoreReasoning",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ScoredAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ActivityCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CargoShipmentId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomsDeclarationNo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DriverSurname",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExemptionCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExportCurrency",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExportExchangeRate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "GibStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "GibStatusDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "GtipCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ParasutEInvoiceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ParasutSalesInvoiceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ParasutSyncError",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ParasutSyncStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ParasutSyncedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ProfessionalTitle",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Scenario",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ShipmentAddress",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ShipmentDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignatureStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignatureType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignedBy",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VehiclePlate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "WaybillNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "WithholdingAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "WithholdingRate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CommissionAmount",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "ShippingCost",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "ErrorDetails",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "FailCount",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "SkipCount",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "SuccessCount",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "TotalRecords",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "TriggeredBy",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "e_invoice_send_logs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "e_invoice_lines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "e_invoice_documents");

            migrationBuilder.DropColumn(
                name: "WithholdingAmount",
                table: "e_invoice_documents");

            migrationBuilder.DropColumn(
                name: "WithholdingRate",
                table: "e_invoice_documents");

            migrationBuilder.DropColumn(
                name: "ReliabilityColor",
                table: "DropshippingPoolProducts");

            migrationBuilder.DropColumn(
                name: "ReliabilityScore",
                table: "DropshippingPoolProducts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "AutoSyncInvoice",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "AutoSyncStock",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ErpProvider",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "IsErpConnected",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PriceSyncPeriodMinutes",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "StockSyncPeriodMinutes",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CircuitStateLogs");

            migrationBuilder.DropColumn(
                name: "InternalCategoryPath",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropColumn(
                name: "IsAutoMapped",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropColumn(
                name: "MappedAt",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropColumn(
                name: "MappedBy",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropColumn(
                name: "MatchConfidence",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropColumn(
                name: "PlatformCategoryPath",
                table: "CategoryPlatformMappings");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "ReminderDate",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CalendarEventAttendees");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Bitrix24DealProductRows");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BarcodeScanLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApiCallLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AccessLogs");

            migrationBuilder.RenameIndex(
                name: "IX_Quotations_Tenant_Status",
                table: "Quotations",
                newName: "IX_Quotations_TenantId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_KontorBalances_Store_Provider",
                table: "KontorBalances",
                newName: "IX_KontorBalances_StoreId_Provider");

            migrationBuilder.RenameIndex(
                name: "IX_erp_sync_logs_TenantId",
                table: "erp_sync_logs",
                newName: "IX_ErpSyncLogs_TenantId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "WorkTasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemperatureRange",
                table: "WarehouseZones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WarehouseZones",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "HumidityRange",
                table: "WarehouseZones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseZones",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingSection",
                table: "WarehouseZones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WarehouseShelves",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseShelves",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Accessibility",
                table: "WarehouseShelves",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UsableArea",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Warehouses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalArea",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OperatingHours",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Warehouses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyCost",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinTemperature",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinHumidity",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxTemperature",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxHumidity",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxCapacity",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Height",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPerSquareMeter",
                table: "Warehouses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CostCenter",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPerson",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Warehouses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CapacityUnit",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Warehouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RackType",
                table: "WarehouseRacks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Orientation",
                table: "WarehouseRacks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WarehouseRacks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseRacks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WarehouseBins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseBins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "BinType",
                table: "WarehouseBins",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TimeEntries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SyncType",
                table: "SyncRetryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "LastError",
                table: "SyncRetryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "ItemType",
                table: "SyncRetryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                table: "SyncRetryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorCategory",
                table: "SyncRetryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "SyncRetryItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalInfo",
                table: "SyncRetryItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlatformCode",
                table: "SyncLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "SyncLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "SyncLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "SyncLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "SyncLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VatNumber",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TradeRegisterNumber",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxOffice",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxNumber",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Suppliers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Fax",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentUrls",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountRate",
                table: "Suppliers",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentBalance",
                table: "Suppliers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Suppliers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditLimit",
                table: "Suppliers",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPerson",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Suppliers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Suppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierTaxOffice",
                table: "SupplierAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierTaxNumber",
                table: "SupplierAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierPhone",
                table: "SupplierAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierName",
                table: "SupplierAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierEmail",
                table: "SupplierAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierAddress",
                table: "SupplierAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SupplierAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "AccountCode",
                table: "SupplierAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Reasoning",
                table: "StockPredictions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "StockMovements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCost",
                table: "StockMovements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "StockMovements",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProcessedBy",
                table: "StockMovements",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastError",
                table: "SocialFeedConfigurations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FeedUrl",
                table: "SocialFeedConfigurations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryFilter",
                table: "SocialFeedConfigurations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "Sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "Sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceInfo",
                table: "Sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrackingNumber",
                table: "ReturnRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReasonDetail",
                table: "ReturnRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlatformReturnId",
                table: "ReturnRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ReturnRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "ReturnRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "ReturnRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "ReturnRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "ReturnRequestLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "ReturnRequestLines",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RefundAmount",
                table: "ReturnRequestLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "ReturnRequestLines",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ReturnRequestLines",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Terms",
                table: "Quotations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Quotations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerTaxOffice",
                table: "Quotations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerTaxNumber",
                table: "Quotations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Quotations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerAddress",
                table: "Quotations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "QuotationLines",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "ProjectMembers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "FulfillmentCenter",
                table: "ProductWarehouseStocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ProductSets",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "Products",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRate",
                table: "Products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SalePrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "Products",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchasePrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ListPrice",
                table: "Products",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Strategy",
                table: "PriceRecommendations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "PriceRecommendations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Reasoning",
                table: "PriceRecommendations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "PlatformPaymentId",
                table: "PlatformPayments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PlatformPayments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PlatformPayments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "BankReference",
                table: "PlatformPayments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "PlatformCommissions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PlatformCategoryId",
                table: "PlatformCommissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PlatformCommissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PlatformCommissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "PlatformCommissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Probability",
                table: "PipelineStages",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Permissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Module",
                table: "Permissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "PaymentTransactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PaymentTransactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PaymentTransactions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "TrackingNumber",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotal",
                table: "Orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PlatformOrderNumber",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalOrderId",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionRate",
                table: "Orders",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldPrecision: 5,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionAmount",
                table: "Orders",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CargoExpenseAmount",
                table: "Orders",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CargoBarcode",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "OrderItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRate",
                table: "OrderItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "OrderItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "ProductSKU",
                table: "OrderItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "OrderItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OfflineQueueItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "LastError",
                table: "OfflineQueueItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                table: "OfflineQueueItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "OfflineQueueItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Channel",
                table: "OfflineQueueItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateName",
                table: "NotificationLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Recipient",
                table: "NotificationLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "NotificationLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "NotificationLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LogEntries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "LogEntries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "LogEntries",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "MachineName",
                table: "LogEntries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "LogEntries",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "LogEntries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Exception",
                table: "LogEntries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "LogEntries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "LogEntries",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Leaves",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Leaves",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxTotal",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotal",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PdfUrl",
                table: "Invoices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GrandTotal",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Invoices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "InvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRate",
                table: "InvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "InvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "LineTotal",
                table: "InvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "InvoiceLines",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingQty",
                table: "InventoryLots",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReceivedQty",
                table: "InventoryLots",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "LotNumber",
                table: "InventoryLots",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Incomes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Incomes",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "FinanceExpenses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "FinanceExpenses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "FinanceExpenses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "FinanceExpenses",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "RecurrencePeriod",
                table: "Expenses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Expenses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Expenses",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "WebsiteUrl",
                table: "DropshipSuppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DropshipSuppliers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "MarkupValue",
                table: "DropshipSuppliers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "DropshipSuppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApiEndpoint",
                table: "DropshipSuppliers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "DropshipProducts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<decimal>(
                name: "SellingPrice",
                table: "DropshipProducts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalPrice",
                table: "DropshipProducts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalUrl",
                table: "DropshipProducts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalProductId",
                table: "DropshipProducts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierTrackingNumber",
                table: "DropshipOrders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierOrderRef",
                table: "DropshipOrders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FailureReason",
                table: "DropshipOrders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Documents",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxOffice",
                table: "Customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxNumber",
                table: "Customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Customers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BillingAddress",
                table: "Customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerTaxOffice",
                table: "CustomerAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerTaxNumber",
                table: "CustomerAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "CustomerAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "CustomerAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "CustomerAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerAddress",
                table: "CustomerAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "CustomerAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditLimit",
                table: "CustomerAccounts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "AccountCode",
                table: "CustomerAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TaxOffice",
                table: "CrmContacts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "CrmContacts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "CrmContacts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CrmContacts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxNumber",
                table: "CompanySettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "CompanySettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "CompanySettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "CompanySettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CompanySettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RateSource",
                table: "CommissionRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "CircuitStateLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "PreviousState",
                table: "CircuitStateLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "NewState",
                table: "CircuitStateLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "CircuitStateLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalInfo",
                table: "CircuitStateLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CariHareketler",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CalendarEvents",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ValidationMessage",
                table: "BarcodeScanLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "BarcodeScanLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "BarcodeScanLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "BarcodeScanLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "BarcodeScanLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "BarcodeScanLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "ApiCallLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Endpoint",
                table: "ApiCallLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "ApiCallLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ApiCallLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentNumber",
                table: "AccountTransactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AccountTransactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "AccountTransactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerAccountId",
                table: "AccountTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierAccountId",
                table: "AccountTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "AccountingDocuments",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AccessLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Resource",
                table: "AccessLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "AccessLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "AccessLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalInfo",
                table: "AccessLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AccessLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSetItems_ProductSetId",
                table: "ProductSetItems",
                column: "ProductSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformCommissions_Tenant_Platform_Category",
                table: "PlatformCommissions",
                columns: new[] { "TenantId", "Platform", "PlatformCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryId",
                table: "Categories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_CustomerAccountId",
                table: "AccountTransactions",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_SupplierAccountId",
                table: "AccountTransactions",
                column: "SupplierAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountTransactions_CustomerAccounts_CustomerAccountId",
                table: "AccountTransactions",
                column: "CustomerAccountId",
                principalTable: "CustomerAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountTransactions_SupplierAccounts_SupplierAccountId",
                table: "AccountTransactions",
                column: "SupplierAccountId",
                principalTable: "SupplierAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_CategoryId",
                table: "Categories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerAccounts_Customers_CustomerId",
                table: "CustomerAccounts",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Employees_EmployeeId",
                table: "Leaves",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformPayments_Stores_StoreId",
                table: "PlatformPayments",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Customers_CustomerId",
                table: "Quotations",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequestLines_Products_ProductId",
                table: "ReturnRequestLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequests_Orders_OrderId",
                table: "ReturnRequests",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequests_Stores_StoreId",
                table: "ReturnRequests",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierAccounts_Suppliers_SupplierId",
                table: "SupplierAccounts",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }
    }
}
