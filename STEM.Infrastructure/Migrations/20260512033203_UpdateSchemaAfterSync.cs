using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaAfterSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendees_Users_StudentId",
                table: "Attendees");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Users_StudentId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendees_Users_StudentId",
                table: "Attendees",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Users_StudentId",
                table: "Certificates",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendees_Users_StudentId",
                table: "Attendees");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Users_StudentId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendees_Users_StudentId",
                table: "Attendees",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Users_StudentId",
                table: "Certificates",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
