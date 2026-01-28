using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using SuperEcomManager.Domain.Enums;

#nullable disable

namespace SuperEcomManager.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class FixWebhookEventsColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert jsonb array to integer array
            // This uses the translate function to convert jsonb array to PostgreSQL array
            migrationBuilder.Sql(@"
                ALTER TABLE webhook_subscriptions
                ALTER COLUMN ""Events""
                TYPE integer[]
                USING translate(""Events""::text, '[]', '{}')::integer[];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<WebhookEvent>>(
                name: "Events",
                table: "webhook_subscriptions",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(int[]),
                oldType: "integer[]");
        }
    }
}
