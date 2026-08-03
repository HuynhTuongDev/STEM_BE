using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestructureCourseClassQuizVirtualLabRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Users_TeacherId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Courses_CourseId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Simulations_Lessons_LessonId",
                table: "Simulations");

            migrationBuilder.DropIndex(
                name: "IX_Courses_TeacherId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "LessonId",
                table: "Simulations",
                newName: "ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_Simulations_LessonId",
                table: "Simulations",
                newName: "IX_Simulations_ClassId");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "Quizzes",
                newName: "ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_Quizzes_CourseId",
                table: "Quizzes",
                newName: "IX_Quizzes_ClassId");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Simulations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiagramJson",
                table: "Simulations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Simulations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Classes_ClassId",
                table: "Quizzes",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Simulations_Classes_ClassId",
                table: "Simulations",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Classes_ClassId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Simulations_Classes_ClassId",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "DiagramJson",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Simulations");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "Simulations",
                newName: "LessonId");

            migrationBuilder.RenameIndex(
                name: "IX_Simulations_ClassId",
                table: "Simulations",
                newName: "IX_Simulations_LessonId");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "Quizzes",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_Quizzes_ClassId",
                table: "Quizzes",
                newName: "IX_Quizzes_CourseId");

            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TeacherId",
                table: "Courses",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Users_TeacherId",
                table: "Courses",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Courses_CourseId",
                table: "Quizzes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Simulations_Lessons_LessonId",
                table: "Simulations",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
