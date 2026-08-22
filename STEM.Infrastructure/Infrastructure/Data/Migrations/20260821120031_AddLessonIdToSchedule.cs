using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonIdToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "Schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_LessonId",
                table: "Schedules",
                column: "LessonId",
                unique: true,
                filter: "\"LessonId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Lessons_LessonId",
                table: "Schedules",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Lessons_LessonId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_LessonId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "Schedules");
        }
    }
}
