using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_FileEntities_FileId",
                table: "Submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FileEntities",
                table: "FileEntities");

            migrationBuilder.RenameTable(
                name: "FileEntities",
                newName: "FileEntity");

            migrationBuilder.AlterColumn<string>(
                name: "ContentJson",
                table: "Submissions",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AutoGradeResultJson",
                table: "Submissions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileEntity",
                table: "FileEntity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Files_FileId",
                table: "Submissions",
                column: "FileId",
                principalTable: "FileEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Files_FileId",
                table: "Submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FileEntity",
                table: "FileEntity");

            migrationBuilder.RenameTable(
                name: "FileEntity",
                newName: "FileEntities");

            migrationBuilder.AlterColumn<string>(
                name: "ContentJson",
                table: "Submissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "AutoGradeResultJson",
                table: "Submissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileEntities",
                table: "FileEntities",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_FileEntities_FileId",
                table: "Submissions",
                column: "FileId",
                principalTable: "FileEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
