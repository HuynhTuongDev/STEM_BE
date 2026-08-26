using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentLimitToPaymentPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentLimit",
                table: "PaymentPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "StudentLimit" },
                values: new object[] { "Dành cho trường có quy mô nhỏ", 50 });

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "StudentLimit" },
                values: new object[] { "Dành cho trường có quy mô trung bình", 200 });

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Features", "StudentLimit" },
                values: new object[] { "Dành cho trường có quy mô lớn", "[\"Hỗ trợ AI toàn diện\", \"Tối đa 500 học sinh\", \"Báo cáo & phân tích nâng cao\", \"Hỗ trợ 24/7\"]", 500 });

            migrationBuilder.InsertData(
                table: "PaymentPackages",
                columns: new[] { "Id", "CreatedAt", "Currency", "Description", "DisplayOrder", "ExpiresAt", "Features", "IsActive", "IsFeatured", "Name", "Price", "StudentLimit", "TokenAmount", "UpdatedAt" },
                values: new object[] { 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VND", "Không giới hạn học sinh", 4, new DateTime(2026, 8, 31, 23, 59, 59, 0, DateTimeKind.Utc), "[\"Hỗ trợ AI toàn diện\", \"Không giới hạn học sinh\", \"Báo cáo & phân tích nâng cao\", \"Hỗ trợ 24/7\", \"API tùy chỉnh\"]", true, false, "Unlimited", 1999000m, 999999, 50000000, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "StudentLimit",
                table: "PaymentPackages");

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Gói dành cho trường mới bắt đầu");

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Gói phổ biến cho trường có quy mô trung bình");

            migrationBuilder.UpdateData(
                table: "PaymentPackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Features" },
                values: new object[] { "Gói đầy đủ tính năng cho trường lớn", "[\"Hỗ trợ AI toàn diện\", \"Không giới hạn học sinh\", \"Báo cáo & phân tích nâng cao\", \"Hỗ trợ 24/7\", \"API tùy chỉnh\"]" });
        }
    }
}
