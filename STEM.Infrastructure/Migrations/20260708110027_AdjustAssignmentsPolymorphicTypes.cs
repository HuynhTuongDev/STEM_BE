using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdjustAssignmentsPolymorphicTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "FileId",
                table: "Submissions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "AutoGradeResultJson",
                table: "Submissions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AutoScore",
                table: "Submissions",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentJson",
                table: "Submissions",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<decimal>(
                name: "FinalScore",
                table: "Submissions",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Submissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "submitted");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Submissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowResubmit",
                table: "Assignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentType",
                table: "Assignments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "text_report");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Assignments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "Assignments",
                type: "numeric(6,2)",
                nullable: false,
                defaultValue: 100m);

            migrationBuilder.AddColumn<int>(
                name: "ResubmitLimit",
                table: "Assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RubricId",
                table: "Assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Assignments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "draft");

            migrationBuilder.CreateTable(
                name: "AssignmentQuizDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    QuestionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TimeLimitSeconds = table.Column<int>(type: "integer", nullable: true),
                    ShuffleQuestions = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentQuizDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentQuizDetails_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentReportDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: false),
                    AllowedSubmissionTypesJson = table.Column<string>(type: "jsonb", nullable: false),
                    AllowedFileExtensionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    MaxFileSizeMb = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentReportDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentReportDetails_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentSimulationDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentSource = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BaseDiagramJson = table.Column<string>(type: "jsonb", nullable: false),
                    AllowedComponentTypesJson = table.Column<string>(type: "jsonb", nullable: false),
                    StudentInputMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StarterCode = table.Column<string>(type: "text", nullable: true),
                    AnswerKeyJson = table.Column<string>(type: "jsonb", nullable: false),
                    AutoGradingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoGradingWeight = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentSimulationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentSimulationDetails_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentQuizDetails_AssignmentId",
                table: "AssignmentQuizDetails",
                column: "AssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentReportDetails_AssignmentId",
                table: "AssignmentReportDetails",
                column: "AssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSimulationDetails_AssignmentId",
                table: "AssignmentSimulationDetails",
                column: "AssignmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentQuizDetails");

            migrationBuilder.DropTable(
                name: "AssignmentReportDetails");

            migrationBuilder.DropTable(
                name: "AssignmentSimulationDetails");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AutoGradeResultJson",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AutoScore",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ContentJson",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AllowResubmit",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "AssignmentType",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ResubmitLimit",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "RubricId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Assignments");

            migrationBuilder.AlterColumn<int>(
                name: "FileId",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
