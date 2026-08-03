using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceOutboundQueueWithUnderProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Histories_AspNetUsers_CreatedByUserId",
                table: "Histories");

            migrationBuilder.DropTable(
                name: "OutboundMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Histories",
                table: "Histories");

            migrationBuilder.RenameTable(
                name: "Histories",
                newName: "historys");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "historys",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "historys",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "historys",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "historys",
                newName: "message_type");

            migrationBuilder.RenameColumn(
                name: "SenderNumber",
                table: "historys",
                newName: "sender_no");

            migrationBuilder.RenameColumn(
                name: "ReceiverNumber",
                table: "historys",
                newName: "receiver_no");

            migrationBuilder.RenameColumn(
                name: "MessageText",
                table: "historys",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "GatewayResponse",
                table: "historys",
                newName: "response");

            migrationBuilder.RenameColumn(
                name: "ExternalMessageId",
                table: "historys",
                newName: "get_message_id");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "historys",
                newName: "creater_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "historys",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CampaignBatchId",
                table: "historys",
                newName: "camp_id");

            migrationBuilder.RenameIndex(
                name: "IX_Histories_ExternalMessageId",
                table: "historys",
                newName: "IX_historys_get_message_id");

            migrationBuilder.RenameIndex(
                name: "IX_Histories_CreatedByUserId_CreatedAt",
                table: "historys",
                newName: "IX_historys_creater_id_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Histories_CampaignBatchId",
                table: "historys",
                newName: "IX_historys_camp_id");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "historys",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "message_type",
                table: "historys",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            // camp_id narrows to the daemon's 25-char width. Rows predating this migration carry
            // 32-char GUID batch ids from the old in-process sender, which MySQL refuses to
            // truncate implicitly under strict mode - so shorten them explicitly first. They are
            // only ever read back as an opaque grouping key, so a shortened id stays usable.
            migrationBuilder.Sql("UPDATE historys SET camp_id = LEFT(camp_id, 25) WHERE LENGTH(camp_id) > 25;");

            migrationBuilder.AlterColumn<string>(
                name: "camp_id",
                table: "historys",
                type: "varchar(25)",
                maxLength: 25,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "camp_name",
                table: "historys",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // The AlterColumns above widen int enum ordinals into text, which MySQL renders as
            // "0"/"1"/... - meaningless to the SMPP daemon that now reads these columns. Translate
            // any pre-existing rows to the codes it actually speaks.
            migrationBuilder.Sql(
                "UPDATE historys SET status = ELT(CAST(status AS UNSIGNED) + 1, " +
                "'PROCESS','SENT','DELIVRD','UNDELIV','EXPIRED','FAILED') WHERE status REGEXP '^[0-5]$';");
            migrationBuilder.Sql(
                "UPDATE historys SET message_type = ELT(CAST(message_type AS UNSIGNED) + 1, " +
                "'STXTM','BTXTM','ATXTM') WHERE message_type REGEXP '^[0-2]$';");

            migrationBuilder.AddPrimaryKey(
                name: "PK_historys",
                table: "historys",
                column: "id");

            migrationBuilder.CreateTable(
                name: "under_process",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    camp_id = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    senderId = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    camp_numbers = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    camp_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    push_type = table.Column<int>(type: "int", nullable: false),
                    userId = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    file_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_under_process", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_under_process_camp_id",
                table: "under_process",
                column: "camp_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_historys_AspNetUsers_creater_id",
                table: "historys",
                column: "creater_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_historys_AspNetUsers_creater_id",
                table: "historys");

            migrationBuilder.DropTable(
                name: "under_process");

            migrationBuilder.DropPrimaryKey(
                name: "PK_historys",
                table: "historys");

            migrationBuilder.DropColumn(
                name: "camp_name",
                table: "historys");

            migrationBuilder.RenameTable(
                name: "historys",
                newName: "Histories");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Histories",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Histories",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Histories",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "sender_no",
                table: "Histories",
                newName: "SenderNumber");

            migrationBuilder.RenameColumn(
                name: "response",
                table: "Histories",
                newName: "GatewayResponse");

            migrationBuilder.RenameColumn(
                name: "receiver_no",
                table: "Histories",
                newName: "ReceiverNumber");

            migrationBuilder.RenameColumn(
                name: "message_type",
                table: "Histories",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "Histories",
                newName: "MessageText");

            migrationBuilder.RenameColumn(
                name: "get_message_id",
                table: "Histories",
                newName: "ExternalMessageId");

            migrationBuilder.RenameColumn(
                name: "creater_id",
                table: "Histories",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Histories",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "camp_id",
                table: "Histories",
                newName: "CampaignBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_historys_get_message_id",
                table: "Histories",
                newName: "IX_Histories_ExternalMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_historys_creater_id_created_at",
                table: "Histories",
                newName: "IX_Histories_CreatedByUserId_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_historys_camp_id",
                table: "Histories",
                newName: "IX_Histories_CampaignBatchId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Histories",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Source",
                table: "Histories",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "CampaignBatchId",
                table: "Histories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(25)",
                oldMaxLength: 25)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Histories",
                table: "Histories",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "OutboundMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CampaignBatchId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    HistoryId = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageText = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceiverNumber = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenderNumber = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundMessages_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundMessages_CampaignBatchId",
                table: "OutboundMessages",
                column: "CampaignBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundMessages_CreatedByUserId",
                table: "OutboundMessages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundMessages_Status",
                table: "OutboundMessages",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Histories_AspNetUsers_CreatedByUserId",
                table: "Histories",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
