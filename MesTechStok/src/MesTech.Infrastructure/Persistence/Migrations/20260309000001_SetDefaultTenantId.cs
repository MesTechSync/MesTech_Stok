using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultTenantId : Migration
    {
        // Varsayılan tenant ID — yeni kurulumlar bu ID'yi kullanır.
        private const string DefaultTenantId = "00000000-0000-0000-0000-000000000001";

        // Guid.Empty (atanmamış) veya NULL değerleri varsayılan tenant'a atar.
        // Global Query Filter aktif olmadan önce çalıştırılmalıdır.
        private static readonly string EmptyGuid = "00000000-0000-0000-0000-000000000000";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NULL içerebilen tek tablo: Users (nullable TenantId)
            migrationBuilder.Sql($"""
                UPDATE "Users"
                SET "TenantId" = '{DefaultTenantId}'
                WHERE "TenantId" IS NULL OR "TenantId" = '{EmptyGuid}';
                """);

            // NOT NULL tablolar — Guid.Empty değerlerini düzelt
            foreach (var table in TenantScopedTables)
            {
                migrationBuilder.Sql($"""
                    UPDATE "{table}"
                    SET "TenantId" = '{DefaultTenantId}'
                    WHERE "TenantId" = '{EmptyGuid}';
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri almak güvenli değil — veri kaybı riski yok ama orijinal değerler bilinmiyor.
            // İzleme: bu migration'ı DOWN etmek zorunda kalınırsa manuel müdahale gerekir.
        }

        /// <summary>
        /// ITenantEntity implement eden tüm tablolar.
        /// SyncLogs dahil — TenantId veri sahipliği için tutulur.
        /// </summary>
        private static readonly string[] TenantScopedTables =
        [
            "Products",
            "Categories",
            "Orders",
            "OrderItems",
            "Customers",
            "Suppliers",
            "Warehouses",
            "Brands",
            "Stores",
            "StockMovements",
            "InventoryLots",
            "ProductVariants",
            "CompanySettings",
            "Invoices",
            "SyncLogs",
            "BrandPlatformMappings",
            "CategoryPlatformMappings",
            "ProductPlatformMappings",
        ];
    }
}
