using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttendanceRecordUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ClassId_StudentId_AttendanceDate",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ScheduleId_StudentId",
                table: "AttendanceRecords",
                columns: new[] { "ScheduleId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ScheduleId_StudentId",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ClassId_StudentId_AttendanceDate",
                table: "AttendanceRecords",
                columns: new[] { "ClassId", "StudentId", "AttendanceDate" },
                unique: true);
        }
    }
}
