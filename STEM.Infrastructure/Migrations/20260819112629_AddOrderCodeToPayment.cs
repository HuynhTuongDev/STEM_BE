using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCodeToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OrderCode",
                table: "Payments",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderCode",
                table: "Payments");
        }
    }
}
