using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomSandboxLabsAndCompileSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WokwiProjectId",
                table: "Labs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "AllowedComponentTypesJson",
                table: "Labs",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "BoardType",
                table: "Labs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "arduino_uno");

            migrationBuilder.AddColumn<string>(
                name: "CircuitConfigJson",
                table: "Labs",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "SimulationMode",
                table: "Labs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "wokwi_iframe");

            migrationBuilder.AddColumn<string>(
                name: "StarterCode",
                table: "Labs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComponentGlueRegistry",
                columns: table => new
                {
                    ComponentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Supported = table.Column<bool>(type: "boolean", nullable: false),
                    PinRequirementsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentGlueRegistry", x => x.ComponentType);
                });

            migrationBuilder.InsertData(
                table: "ComponentGlueRegistry",
                columns: new[] { "ComponentType", "CreatedAt", "Label", "PinRequirementsJson", "Supported", "UpdatedAt" },
                values: new object[,]
                {
                    { "buzzer", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Buzzer", "{\"pins\":[{\"name\":\"pin\",\"kind\":\"pwm_output\"}]}", true, new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "dht22", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), "DHT22", "{\"pins\":[{\"name\":\"data\",\"kind\":\"digital_bidirectional\"}]}", false, new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "led", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), "LED", "{\"pins\":[{\"name\":\"pin\",\"kind\":\"digital_output\"}]}", true, new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "potentiometer", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Potentiometer", "{\"pins\":[{\"name\":\"pin\",\"kind\":\"analog_input\"}]}", true, new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "push_button", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Push Button", "{\"pins\":[{\"name\":\"pin\",\"kind\":\"digital_input\"}]}", true, new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "servo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Servo", "{\"pins\":[{\"name\":\"pin\",\"kind\":\"pwm_output\"}]}", true, new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentGlueRegistry");

            migrationBuilder.DropColumn(
                name: "AllowedComponentTypesJson",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "BoardType",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "CircuitConfigJson",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "SimulationMode",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "StarterCode",
                table: "Labs");

            migrationBuilder.AlterColumn<string>(
                name: "WokwiProjectId",
                table: "Labs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);
        }
    }
}
