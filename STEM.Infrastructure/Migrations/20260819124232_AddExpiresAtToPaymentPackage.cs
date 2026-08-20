using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiresAtToPaymentPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationMonths",
                table: "PaymentPackages");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "PaymentPackages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ExpiresAt", "TokenAmount" },
                values: new object[] { new DateTime(2026, 8, 31, 23, 59, 59, 0, DateTimeKind.Utc), 4000000 });

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ExpiresAt", "TokenAmount" },
                values: new object[] { new DateTime(2026, 8, 31, 23, 59, 59, 0, DateTimeKind.Utc), 8000000 });

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ExpiresAt", "TokenAmount" },
                values: new object[] { new DateTime(2026, 8, 31, 23, 59, 59, 0, DateTimeKind.Utc), 16000000 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "PaymentPackages");

            migrationBuilder.AddColumn<int>(
                name: "DurationMonths",
                table: "PaymentPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DurationMonths", "TokenAmount" },
                values: new object[] { 3, 1000 });

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DurationMonths", "TokenAmount" },
                values: new object[] { 6, 2500 });

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DurationMonths", "TokenAmount" },
                values: new object[] { 12, 6000 });
        }
    }
}
