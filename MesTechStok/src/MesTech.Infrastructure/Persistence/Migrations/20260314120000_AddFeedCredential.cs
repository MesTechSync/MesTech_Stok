using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ENT-DROP-IMP-SPRINT-D D-07: Şifrelenmiş HTTP Basic Auth credential alanı
            migrationBuilder.AddColumn<string>(
                name: "EncryptedCredential",
                table: "SupplierFeeds",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedCredential",
                table: "SupplierFeeds");
        }
    }
}
