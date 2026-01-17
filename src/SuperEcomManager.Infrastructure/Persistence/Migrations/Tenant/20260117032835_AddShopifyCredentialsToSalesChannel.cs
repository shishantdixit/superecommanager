using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperEcomManager.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddShopifyCredentialsToSalesChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to handle both fresh migrations and existing schemas
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sales_channels' AND column_name = 'AccessToken') THEN
                        ALTER TABLE sales_channels ADD COLUMN ""AccessToken"" text;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sales_channels' AND column_name = 'ApiKey') THEN
                        ALTER TABLE sales_channels ADD COLUMN ""ApiKey"" text;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sales_channels' AND column_name = 'ApiSecret') THEN
                        ALTER TABLE sales_channels ADD COLUMN ""ApiSecret"" text;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sales_channels' AND column_name = 'IsConnected') THEN
                        ALTER TABLE sales_channels ADD COLUMN ""IsConnected"" boolean NOT NULL DEFAULT false;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sales_channels' AND column_name = 'LastConnectedAt') THEN
                        ALTER TABLE sales_channels ADD COLUMN ""LastConnectedAt"" timestamp with time zone;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sales_channels' AND column_name = 'LastError') THEN
                        ALTER TABLE sales_channels ADD COLUMN ""LastError"" text;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'sales_channels' AND column_name = 'Scopes') THEN
                        ALTER TABLE sales_channels ADD COLUMN ""Scopes"" text;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessToken",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "ApiSecret",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "IsConnected",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "LastConnectedAt",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "sales_channels");
        }
    }
}
