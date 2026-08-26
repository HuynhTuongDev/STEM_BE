using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScheduleLessonCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_ClassId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_LessonId",
                table: "Schedules");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""IX_Schedules_ClassId_LessonId"" 
                ON ""Schedules"" (""ClassId"", ""LessonId"") 
                WHERE ""LessonId"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_Schedules_ClassId_LessonId"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ClassId",
                table: "Schedules",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_LessonId",
                table: "Schedules",
                column: "LessonId",
                unique: true,
                filter: "LessonId IS NOT NULL");
        }
    }
}
