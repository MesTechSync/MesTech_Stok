using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Dalga16_EntityModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: only rename if old table exists (DB may already have new names from SyncSnapshot)
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_ImportFieldMapping_ImportTemplate_ImportTemplateId') THEN
        ALTER TABLE ""ImportFieldMapping"" DROP CONSTRAINT ""FK_ImportFieldMapping_ImportTemplate_ImportTemplateId"";
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_JournalEntries_Tenant_Reference') THEN
        DROP INDEX ""IX_JournalEntries_Tenant_Reference"";
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'RecurringExpense') THEN
        ALTER TABLE ""RecurringExpense"" DROP CONSTRAINT IF EXISTS ""PK_RecurringExpense"";
        ALTER TABLE ""RecurringExpense"" RENAME TO ""RecurringExpenses"";
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'ProfitLossEntry') THEN
        ALTER TABLE ""ProfitLossEntry"" DROP CONSTRAINT IF EXISTS ""PK_ProfitLossEntry"";
        ALTER TABLE ""ProfitLossEntry"" RENAME TO ""ProfitLossEntries"";
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'ImportTemplate') THEN
        ALTER TABLE ""ImportTemplate"" DROP CONSTRAINT IF EXISTS ""PK_ImportTemplate"";
        ALTER TABLE ""ImportTemplate"" RENAME TO ""ImportTemplates"";
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'ImportFieldMapping') THEN
        ALTER TABLE ""ImportFieldMapping"" DROP CONSTRAINT IF EXISTS ""PK_ImportFieldMapping"";
        ALTER TABLE ""ImportFieldMapping"" RENAME TO ""ImportFieldMappings"";
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'HepsiburadaListing') THEN
        ALTER TABLE ""HepsiburadaListing"" DROP CONSTRAINT IF EXISTS ""PK_HepsiburadaListing"";
        ALTER TABLE ""HepsiburadaListing"" RENAME TO ""HepsiburadaListings"";
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'CiceksepetiCategory') THEN
        ALTER TABLE ""CiceksepetiCategory"" DROP CONSTRAINT IF EXISTS ""PK_CiceksepetiCategory"";
        ALTER TABLE ""CiceksepetiCategory"" RENAME TO ""CiceksepetiCategories"";
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'BudgetPlan') THEN
        ALTER TABLE ""BudgetPlan"" DROP CONSTRAINT IF EXISTS ""PK_BudgetPlan"";
        ALTER TABLE ""BudgetPlan"" RENAME TO ""BudgetPlans"";
    END IF;
END $$;");

            // Rename indexes — only if old name exists
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_RecurringExpense_TenantId') THEN ALTER INDEX ""IX_RecurringExpense_TenantId"" RENAME TO ""IX_RecurringExpenses_TenantId""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_RecurringExpense_NextDueDate') THEN ALTER INDEX ""IX_RecurringExpense_NextDueDate"" RENAME TO ""IX_RecurringExpenses_NextDueDate""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_RecurringExpense_IsDeleted') THEN ALTER INDEX ""IX_RecurringExpense_IsDeleted"" RENAME TO ""IX_RecurringExpenses_IsDeleted""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_RecurringExpense_IsActive') THEN ALTER INDEX ""IX_RecurringExpense_IsActive"" RENAME TO ""IX_RecurringExpenses_IsActive""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_ProfitLossEntry_TenantId') THEN ALTER INDEX ""IX_ProfitLossEntry_TenantId"" RENAME TO ""IX_ProfitLossEntries_TenantId""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_ProfitLossEntry_IsDeleted') THEN ALTER INDEX ""IX_ProfitLossEntry_IsDeleted"" RENAME TO ""IX_ProfitLossEntries_IsDeleted""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_ImportTemplate_TenantId') THEN ALTER INDEX ""IX_ImportTemplate_TenantId"" RENAME TO ""IX_ImportTemplates_TenantId""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_ImportTemplate_IsDeleted') THEN ALTER INDEX ""IX_ImportTemplate_IsDeleted"" RENAME TO ""IX_ImportTemplates_IsDeleted""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_ImportFieldMapping_IsDeleted') THEN ALTER INDEX ""IX_ImportFieldMapping_IsDeleted"" RENAME TO ""IX_ImportFieldMappings_IsDeleted""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_ImportFieldMapping_ImportTemplateId') THEN ALTER INDEX ""IX_ImportFieldMapping_ImportTemplateId"" RENAME TO ""IX_ImportFieldMappings_ImportTemplateId""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_HepsiburadaListing_TenantId') THEN ALTER INDEX ""IX_HepsiburadaListing_TenantId"" RENAME TO ""IX_HepsiburadaListings_TenantId""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_HepsiburadaListing_IsDeleted') THEN ALTER INDEX ""IX_HepsiburadaListing_IsDeleted"" RENAME TO ""IX_HepsiburadaListings_IsDeleted""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CiceksepetiCategory_TenantId') THEN ALTER INDEX ""IX_CiceksepetiCategory_TenantId"" RENAME TO ""IX_CiceksepetiCategories_TenantId""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_CiceksepetiCategory_IsDeleted') THEN ALTER INDEX ""IX_CiceksepetiCategory_IsDeleted"" RENAME TO ""IX_CiceksepetiCategories_IsDeleted""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_BudgetPlan_TenantId') THEN ALTER INDEX ""IX_BudgetPlan_TenantId"" RENAME TO ""IX_BudgetPlans_TenantId""; END IF;
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_BudgetPlan_IsDeleted') THEN ALTER INDEX ""IX_BudgetPlan_IsDeleted"" RENAME TO ""IX_BudgetPlans_IsDeleted""; END IF;
END $$;");

            // Add TenantId columns only if not present (idempotent)
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Roles' AND column_name='TenantId') THEN
        ALTER TABLE ""Roles"" ADD COLUMN ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='RolePermissions' AND column_name='TenantId') THEN
        ALTER TABLE ""RolePermissions"" ADD COLUMN ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProjectMembers' AND column_name='TenantId') THEN
        ALTER TABLE ""ProjectMembers"" ADD COLUMN ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Permissions' AND column_name='TenantId') THEN
        ALTER TABLE ""Permissions"" ADD COLUMN ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $$;");

            // Add PKs only if not already present (idempotent)
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK_RecurringExpenses') AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'RecurringExpenses') THEN
        ALTER TABLE ""RecurringExpenses"" ADD CONSTRAINT ""PK_RecurringExpenses"" PRIMARY KEY (""Id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK_ProfitLossEntries') AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'ProfitLossEntries') THEN
        ALTER TABLE ""ProfitLossEntries"" ADD CONSTRAINT ""PK_ProfitLossEntries"" PRIMARY KEY (""Id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK_ImportTemplates') AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'ImportTemplates') THEN
        ALTER TABLE ""ImportTemplates"" ADD CONSTRAINT ""PK_ImportTemplates"" PRIMARY KEY (""Id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK_ImportFieldMappings') AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'ImportFieldMappings') THEN
        ALTER TABLE ""ImportFieldMappings"" ADD CONSTRAINT ""PK_ImportFieldMappings"" PRIMARY KEY (""Id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK_HepsiburadaListings') AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'HepsiburadaListings') THEN
        ALTER TABLE ""HepsiburadaListings"" ADD CONSTRAINT ""PK_HepsiburadaListings"" PRIMARY KEY (""Id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK_CiceksepetiCategories') AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'CiceksepetiCategories') THEN
        ALTER TABLE ""CiceksepetiCategories"" ADD CONSTRAINT ""PK_CiceksepetiCategories"" PRIMARY KEY (""Id"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK_BudgetPlans') AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'BudgetPlans') THEN
        ALTER TABLE ""BudgetPlans"" ADD CONSTRAINT ""PK_BudgetPlans"" PRIMARY KEY (""Id"");
    END IF;
END $$;");

            // Create indexes idempotently
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Roles_TenantId') THEN
        CREATE INDEX ""IX_Roles_TenantId"" ON ""Roles"" (""TenantId"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_RolePermissions_TenantId') THEN
        CREATE INDEX ""IX_RolePermissions_TenantId"" ON ""RolePermissions"" (""TenantId"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_ProjectMembers_TenantId') THEN
        CREATE INDEX ""IX_ProjectMembers_TenantId"" ON ""ProjectMembers"" (""TenantId"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Permissions_TenantId') THEN
        CREATE INDEX ""IX_Permissions_TenantId"" ON ""Permissions"" (""TenantId"");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_JournalEntries_Tenant_Reference') THEN
        CREATE UNIQUE INDEX ""IX_JournalEntries_Tenant_Reference"" ON ""JournalEntries"" (""TenantId"", ""ReferenceNumber"") WHERE ""ReferenceNumber"" IS NOT NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_ImportFieldMappings_ImportTemplates_ImportTemplateId')
       AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'ImportFieldMappings')
       AND EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'ImportTemplates') THEN
        ALTER TABLE ""ImportFieldMappings"" ADD CONSTRAINT ""FK_ImportFieldMappings_ImportTemplates_ImportTemplateId""
            FOREIGN KEY (""ImportTemplateId"") REFERENCES ""ImportTemplates""(""Id"") ON DELETE CASCADE;
    END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportFieldMappings_ImportTemplates_ImportTemplateId",
                table: "ImportFieldMappings");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_TenantId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_TenantId",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_TenantId",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_Tenant_Reference",
                table: "JournalEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecurringExpenses",
                table: "RecurringExpenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfitLossEntries",
                table: "ProfitLossEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportTemplates",
                table: "ImportTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportFieldMappings",
                table: "ImportFieldMappings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HepsiburadaListings",
                table: "HepsiburadaListings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CiceksepetiCategories",
                table: "CiceksepetiCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BudgetPlans",
                table: "BudgetPlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Permissions");

            migrationBuilder.RenameTable(
                name: "RecurringExpenses",
                newName: "RecurringExpense");

            migrationBuilder.RenameTable(
                name: "ProfitLossEntries",
                newName: "ProfitLossEntry");

            migrationBuilder.RenameTable(
                name: "ImportTemplates",
                newName: "ImportTemplate");

            migrationBuilder.RenameTable(
                name: "ImportFieldMappings",
                newName: "ImportFieldMapping");

            migrationBuilder.RenameTable(
                name: "HepsiburadaListings",
                newName: "HepsiburadaListing");

            migrationBuilder.RenameTable(
                name: "CiceksepetiCategories",
                newName: "CiceksepetiCategory");

            migrationBuilder.RenameTable(
                name: "BudgetPlans",
                newName: "BudgetPlan");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpenses_TenantId",
                table: "RecurringExpense",
                newName: "IX_RecurringExpense_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpenses_NextDueDate",
                table: "RecurringExpense",
                newName: "IX_RecurringExpense_NextDueDate");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpenses_IsDeleted",
                table: "RecurringExpense",
                newName: "IX_RecurringExpense_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpenses_IsActive",
                table: "RecurringExpense",
                newName: "IX_RecurringExpense_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_ProfitLossEntries_TenantId",
                table: "ProfitLossEntry",
                newName: "IX_ProfitLossEntry_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfitLossEntries_IsDeleted",
                table: "ProfitLossEntry",
                newName: "IX_ProfitLossEntry_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_ImportTemplates_TenantId",
                table: "ImportTemplate",
                newName: "IX_ImportTemplate_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_ImportTemplates_IsDeleted",
                table: "ImportTemplate",
                newName: "IX_ImportTemplate_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_ImportFieldMappings_IsDeleted",
                table: "ImportFieldMapping",
                newName: "IX_ImportFieldMapping_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_ImportFieldMappings_ImportTemplateId",
                table: "ImportFieldMapping",
                newName: "IX_ImportFieldMapping_ImportTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_HepsiburadaListings_TenantId",
                table: "HepsiburadaListing",
                newName: "IX_HepsiburadaListing_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_HepsiburadaListings_IsDeleted",
                table: "HepsiburadaListing",
                newName: "IX_HepsiburadaListing_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_CiceksepetiCategories_TenantId",
                table: "CiceksepetiCategory",
                newName: "IX_CiceksepetiCategory_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_CiceksepetiCategories_IsDeleted",
                table: "CiceksepetiCategory",
                newName: "IX_CiceksepetiCategory_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetPlans_TenantId",
                table: "BudgetPlan",
                newName: "IX_BudgetPlan_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetPlans_IsDeleted",
                table: "BudgetPlan",
                newName: "IX_BudgetPlan_IsDeleted");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecurringExpense",
                table: "RecurringExpense",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfitLossEntry",
                table: "ProfitLossEntry",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportTemplate",
                table: "ImportTemplate",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportFieldMapping",
                table: "ImportFieldMapping",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HepsiburadaListing",
                table: "HepsiburadaListing",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CiceksepetiCategory",
                table: "CiceksepetiCategory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BudgetPlan",
                table: "BudgetPlan",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_Tenant_Reference",
                table: "JournalEntries",
                columns: new[] { "TenantId", "ReferenceNumber" },
                unique: true,
                filter: "\"ReferenceNumber\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportFieldMapping_ImportTemplate_ImportTemplateId",
                table: "ImportFieldMapping",
                column: "ImportTemplateId",
                principalTable: "ImportTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
