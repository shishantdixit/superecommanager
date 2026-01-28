using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperEcomManager.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddExternalIdsToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ExternalOrderId and ExternalShipmentId columns to shipments table
            // These columns may already exist in some schemas, so we use IfNotExists pattern
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'shipments' AND column_name = 'ExternalOrderId') THEN
                        ALTER TABLE ""shipments"" ADD COLUMN ""ExternalOrderId"" VARCHAR(50) NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'shipments' AND column_name = 'ExternalShipmentId') THEN
                        ALTER TABLE ""shipments"" ADD COLUMN ""ExternalShipmentId"" VARCHAR(50) NULL;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS ""IX_shipments_ExternalOrderId"" ON ""shipments"" (""ExternalOrderId"");
                CREATE INDEX IF NOT EXISTS ""IX_shipments_ExternalShipmentId"" ON ""shipments"" (""ExternalShipmentId"");
            ");

            migrationBuilder.CreateTable(
                name: "chat_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalTokensUsed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: true),
                    ToolCallId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ToolName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ToolCalls = table.Column<string>(type: "jsonb", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "chat_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_CreatedAt",
                table: "chat_conversations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_LastMessageAt",
                table: "chat_conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_Status",
                table: "chat_conversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_UserId",
                table: "chat_conversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_UserId_Status",
                table: "chat_conversations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId",
                table: "chat_messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId_Sequence",
                table: "chat_messages",
                columns: new[] { "ConversationId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_CreatedAt",
                table: "chat_messages",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_conversations");

            // Drop ExternalOrderId and ExternalShipmentId columns from shipments table
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_shipments_ExternalShipmentId"";
                DROP INDEX IF EXISTS ""IX_shipments_ExternalOrderId"";
                ALTER TABLE ""shipments"" DROP COLUMN IF EXISTS ""ExternalShipmentId"";
                ALTER TABLE ""shipments"" DROP COLUMN IF EXISTS ""ExternalOrderId"";
            ");
        }
    }
}
