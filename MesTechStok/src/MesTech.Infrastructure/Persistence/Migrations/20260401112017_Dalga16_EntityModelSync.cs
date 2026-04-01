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
            migrationBuilder.DropForeignKey(
                name: "FK_ImportFieldMapping_ImportTemplate_ImportTemplateId",
                table: "ImportFieldMapping");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_Tenant_Reference",
                table: "JournalEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecurringExpense",
                table: "RecurringExpense");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfitLossEntry",
                table: "ProfitLossEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportTemplate",
                table: "ImportTemplate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportFieldMapping",
                table: "ImportFieldMapping");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HepsiburadaListing",
                table: "HepsiburadaListing");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CiceksepetiCategory",
                table: "CiceksepetiCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BudgetPlan",
                table: "BudgetPlan");

            migrationBuilder.RenameTable(
                name: "RecurringExpense",
                newName: "RecurringExpenses");

            migrationBuilder.RenameTable(
                name: "ProfitLossEntry",
                newName: "ProfitLossEntries");

            migrationBuilder.RenameTable(
                name: "ImportTemplate",
                newName: "ImportTemplates");

            migrationBuilder.RenameTable(
                name: "ImportFieldMapping",
                newName: "ImportFieldMappings");

            migrationBuilder.RenameTable(
                name: "HepsiburadaListing",
                newName: "HepsiburadaListings");

            migrationBuilder.RenameTable(
                name: "CiceksepetiCategory",
                newName: "CiceksepetiCategories");

            migrationBuilder.RenameTable(
                name: "BudgetPlan",
                newName: "BudgetPlans");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpense_TenantId",
                table: "RecurringExpenses",
                newName: "IX_RecurringExpenses_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpense_NextDueDate",
                table: "RecurringExpenses",
                newName: "IX_RecurringExpenses_NextDueDate");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpense_IsDeleted",
                table: "RecurringExpenses",
                newName: "IX_RecurringExpenses_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpense_IsActive",
                table: "RecurringExpenses",
                newName: "IX_RecurringExpenses_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_ProfitLossEntry_TenantId",
                table: "ProfitLossEntries",
                newName: "IX_ProfitLossEntries_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfitLossEntry_IsDeleted",
                table: "ProfitLossEntries",
                newName: "IX_ProfitLossEntries_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_ImportTemplate_TenantId",
                table: "ImportTemplates",
                newName: "IX_ImportTemplates_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_ImportTemplate_IsDeleted",
                table: "ImportTemplates",
                newName: "IX_ImportTemplates_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_ImportFieldMapping_IsDeleted",
                table: "ImportFieldMappings",
                newName: "IX_ImportFieldMappings_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_ImportFieldMapping_ImportTemplateId",
                table: "ImportFieldMappings",
                newName: "IX_ImportFieldMappings_ImportTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_HepsiburadaListing_TenantId",
                table: "HepsiburadaListings",
                newName: "IX_HepsiburadaListings_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_HepsiburadaListing_IsDeleted",
                table: "HepsiburadaListings",
                newName: "IX_HepsiburadaListings_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_CiceksepetiCategory_TenantId",
                table: "CiceksepetiCategories",
                newName: "IX_CiceksepetiCategories_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_CiceksepetiCategory_IsDeleted",
                table: "CiceksepetiCategories",
                newName: "IX_CiceksepetiCategories_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetPlan_TenantId",
                table: "BudgetPlans",
                newName: "IX_BudgetPlans_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetPlan_IsDeleted",
                table: "BudgetPlans",
                newName: "IX_BudgetPlans_IsDeleted");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "RolePermissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProjectMembers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecurringExpenses",
                table: "RecurringExpenses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfitLossEntries",
                table: "ProfitLossEntries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportTemplates",
                table: "ImportTemplates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportFieldMappings",
                table: "ImportFieldMappings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HepsiburadaListings",
                table: "HepsiburadaListings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CiceksepetiCategories",
                table: "CiceksepetiCategories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BudgetPlans",
                table: "BudgetPlans",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_TenantId",
                table: "RolePermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_TenantId",
                table: "ProjectMembers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_TenantId",
                table: "Permissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_Tenant_Reference",
                table: "JournalEntries",
                columns: new[] { "TenantId", "ReferenceNumber" },
                unique: true,
                filter: "\"ReferenceNumber\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportFieldMappings_ImportTemplates_ImportTemplateId",
                table: "ImportFieldMappings",
                column: "ImportTemplateId",
                principalTable: "ImportTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
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
                filter: "[ReferenceNumber] IS NOT NULL");

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
