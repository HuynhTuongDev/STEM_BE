using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLabsWokwiEmbed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Labs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    WokwiProjectId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    WokwiProjectUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    LinkedAssignmentId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Labs_Assignments_LinkedAssignmentId",
                        column: x => x.LinkedAssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Labs_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabClassAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabClassAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabClassAssignments_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabClassAssignments_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpenCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabProgresses_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabProgresses_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabClassAssignments_ClassId",
                table: "LabClassAssignments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_LabClassAssignments_LabId_ClassId",
                table: "LabClassAssignments",
                columns: new[] { "LabId", "ClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabProgresses_LabId_StudentId",
                table: "LabProgresses",
                columns: new[] { "LabId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabProgresses_StudentId",
                table: "LabProgresses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_Category",
                table: "Labs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_CreatedById",
                table: "Labs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_LinkedAssignmentId",
                table: "Labs",
                column: "LinkedAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_Status",
                table: "Labs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabClassAssignments");

            migrationBuilder.DropTable(
                name: "LabProgresses");

            migrationBuilder.DropTable(
                name: "Labs");
        }
    }
}
