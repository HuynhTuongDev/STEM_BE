using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeScheduleIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Schedules_ScheduleId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ScheduleId_StudentId",
                table: "AttendanceRecords");

            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "AttendanceRecords",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ScheduleId_StudentId_Unique",
                table: "AttendanceRecords",
                columns: new[] { "ScheduleId", "StudentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Schedules_ScheduleId",
                table: "AttendanceRecords",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Schedules_ScheduleId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ScheduleId_StudentId_Unique",
                table: "AttendanceRecords");

            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "AttendanceRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ScheduleId_StudentId",
                table: "AttendanceRecords",
                columns: new[] { "ScheduleId", "StudentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Schedules_ScheduleId",
                table: "AttendanceRecords",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
