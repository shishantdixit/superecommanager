using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperEcomManager.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddSourceChannelIdToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_channel_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_source_channel_id",
                table: "products",
                column: "source_channel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_source_channel_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "source_channel_id",
                table: "products");
        }
    }
}
