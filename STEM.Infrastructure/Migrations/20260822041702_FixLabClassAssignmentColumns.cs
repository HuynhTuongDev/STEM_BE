using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixLabClassAssignmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa FK cũ nếu tồn tại
            migrationBuilder.Sql(@"
                ALTER TABLE ""LabClassAssignments"" DROP CONSTRAINT IF EXISTS ""FK_LabClassAssignments_Lessons_LessonId"";
                ALTER TABLE ""LabClassAssignments"" DROP COLUMN IF EXISTS ""LessonId"";
            ");

            // Thêm cột ScheduleId nếu chưa có
            migrationBuilder.Sql(@"
                ALTER TABLE ""LabClassAssignments"" ADD COLUMN IF NOT EXISTS ""ScheduleId"" integer;
            ");

            // Xóa FK cũ liên quan đến Schedules nếu tồn tại (để tránh trùng)
            migrationBuilder.Sql(@"
                ALTER TABLE ""LabClassAssignments"" DROP CONSTRAINT IF EXISTS ""FK_LabClassAssignments_Schedules_ScheduleId"";
            ");

            // Tạo FK mới
            migrationBuilder.AddForeignKey(
                name: "FK_LabClassAssignments_Schedules_ScheduleId",
                table: "LabClassAssignments",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabClassAssignments_Schedules_ScheduleId",
                table: "LabClassAssignments");

            migrationBuilder.Sql(@"
                ALTER TABLE ""LabClassAssignments"" DROP COLUMN IF EXISTS ""ScheduleId"";
                ALTER TABLE ""LabClassAssignments"" ADD COLUMN ""LessonId"" integer NOT NULL DEFAULT 0;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_LabClassAssignments_Lessons_LessonId",
                table: "LabClassAssignments",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
