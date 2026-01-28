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
            migrationBuilder.AddColumn<string>(
                name: "ExternalOrderId",
                table: "shipments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalShipmentId",
                table: "shipments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_ExternalOrderId",
                table: "shipments",
                column: "ExternalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_ExternalShipmentId",
                table: "shipments",
                column: "ExternalShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipments_ExternalShipmentId",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "IX_shipments_ExternalOrderId",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ExternalShipmentId",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ExternalOrderId",
                table: "shipments");
        }
    }
}
