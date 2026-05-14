using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIpAndDeviceToLoginHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "LoginHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "LoginHistories",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "LoginHistories");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "LoginHistories");
        }
    }
}
