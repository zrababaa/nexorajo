using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignImportedVariables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportedColumnsJson",
                table: "Campaigns",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RecipientVariablesJson",
                table: "Campaigns",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImportedColumnsJson",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "RecipientVariablesJson",
                table: "Campaigns");
        }
    }
}
