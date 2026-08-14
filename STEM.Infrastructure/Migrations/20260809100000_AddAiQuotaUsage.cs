using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <summary>
    /// Viết tay theo đúng quy ước của migration AddRobotDeliveryKitComponentGlueRegistry
    /// (KHÔNG dùng `dotnet ef migrations add` — công cụ này resolve nhầm design-time factory
    /// SQL Server thay vì Npgsql trong môi trường hiện tại). Migration này chỉ tạo mới bảng
    /// AiQuotaUsage (theo dõi token AI theo user/ngày cho tính năng AI Assistant trong Lab),
    /// không đổi bất kỳ bảng/cột nào khác.
    /// </summary>
    public partial class AddAiQuotaUsage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiQuotaUsage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UsageDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiQuotaUsage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiQuotaUsage_UserId_UsageDate",
                table: "AiQuotaUsage",
                columns: new[] { "UserId", "UsageDate" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AiQuotaUsage");
        }
    }
}
