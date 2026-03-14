using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Dalga8_DropshippingPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── DropshippingPools ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "DropshippingPools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    PricingStrategy = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_DropshippingPools", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPools_TenantId",
                table: "DropshippingPools",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPools_Tenant_Active",
                table: "DropshippingPools",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPools_Tenant_Public",
                table: "DropshippingPools",
                columns: new[] { "TenantId", "IsPublic" });

            // ── DropshippingPoolProducts ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "DropshippingPoolProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoolPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AddedFromFeedId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_DropshippingPoolProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DropshippingPoolProducts_DropshippingPools_PoolId",
                        column: x => x.PoolId,
                        principalTable: "DropshippingPools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DropshippingPoolProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DropshippingPoolProducts_SupplierFeeds_AddedFromFeedId",
                        column: x => x.AddedFromFeedId,
                        principalTable: "SupplierFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPoolProducts_TenantId",
                table: "DropshippingPoolProducts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPoolProducts_Tenant_Pool",
                table: "DropshippingPoolProducts",
                columns: new[] { "TenantId", "PoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPoolProducts_Tenant_Product",
                table: "DropshippingPoolProducts",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "UX_DropshippingPoolProducts_Pool_Product",
                table: "DropshippingPoolProducts",
                columns: new[] { "PoolId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPoolProducts_AddedFromFeedId",
                table: "DropshippingPoolProducts",
                column: "AddedFromFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_DropshippingPoolProducts_ProductId",
                table: "DropshippingPoolProducts",
                column: "ProductId");

            // ── FeedImportLogs ────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "FeedImportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierFeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalProducts = table.Column<int>(type: "integer", nullable: false),
                    NewProducts = table.Column<int>(type: "integer", nullable: false),
                    UpdatedProducts = table.Column<int>(type: "integer", nullable: false),
                    DeactivatedProducts = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_FeedImportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedImportLogs_SupplierFeeds_SupplierFeedId",
                        column: x => x.SupplierFeedId,
                        principalTable: "SupplierFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedImportLogs_TenantId",
                table: "FeedImportLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedImportLogs_Tenant_Feed",
                table: "FeedImportLogs",
                columns: new[] { "TenantId", "SupplierFeedId" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedImportLogs_Feed_StartedAt",
                table: "FeedImportLogs",
                columns: new[] { "SupplierFeedId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedImportLogs_Tenant_Status",
                table: "FeedImportLogs",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedImportLogs_SupplierFeedId",
                table: "FeedImportLogs",
                column: "SupplierFeedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FeedImportLogs");
            migrationBuilder.DropTable(name: "DropshippingPoolProducts");
            migrationBuilder.DropTable(name: "DropshippingPools");
        }
    }
}
