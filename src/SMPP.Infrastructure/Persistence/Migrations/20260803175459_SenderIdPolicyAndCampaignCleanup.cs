using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SenderIdPolicyAndCampaignCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The campaign send-speed window was only ever collected on the form: nothing passed
            // it to the SMPP daemon, so it never throttled anything.
            migrationBuilder.DropColumn(
                name: "SendSpeedMaxSeconds",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "SendSpeedMinSeconds",
                table: "Campaigns");

            // One Sender ID per account becomes the comma-separated list of the ones it may send
            // under, so the column is renamed and widened rather than dropped and replaced - a
            // drop would throw away every Sender ID already assigned.
            migrationBuilder.RenameColumn(
                name: "SenderId",
                table: "AspNetUsers",
                newName: "SenderIds");

            migrationBuilder.AlterColumn<string>(
                name: "SenderIds",
                table: "AspNetUsers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "AllowFreeSenderId",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowFreeSenderId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "SenderIds",
                table: "AspNetUsers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.RenameColumn(
                name: "SenderIds",
                table: "AspNetUsers",
                newName: "SenderId");

            migrationBuilder.AddColumn<int>(
                name: "SendSpeedMaxSeconds",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SendSpeedMinSeconds",
                table: "Campaigns",
                type: "int",
                nullable: true);
        }
    }
}
