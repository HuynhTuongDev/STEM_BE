using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── SimulationTemplates: thêm SimulationName, Description ──────────
            migrationBuilder.AddColumn<string>(
                name: "SimulationName",
                table: "SimulationTemplates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SimulationTemplates",
                type: "text",
                nullable: false,
                defaultValue: "");

            // ── SimulationSessions: thêm TemplateId, EndTime, Status ──────────
            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "SimulationSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "SimulationSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SimulationSessions",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            // FK index cho TemplateId
            migrationBuilder.CreateIndex(
                name: "IX_SimulationSessions_TemplateId",
                table: "SimulationSessions",
                column: "TemplateId");

            // FK constraint SimulationSessions.TemplateId → SimulationTemplates.Id
            migrationBuilder.AddForeignKey(
                name: "FK_SimulationSessions_SimulationTemplates_TemplateId",
                table: "SimulationSessions",
                column: "TemplateId",
                principalTable: "SimulationTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── ExperimentLogs: xoá Log cũ, thêm EventType, Payload, LoggedAt ─
            migrationBuilder.DropColumn(
                name: "Log",
                table: "ExperimentLogs");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "ExperimentLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "ExperimentLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LoggedAt",
                table: "ExperimentLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback ExperimentLogs
            migrationBuilder.DropColumn(name: "EventType", table: "ExperimentLogs");
            migrationBuilder.DropColumn(name: "Payload",   table: "ExperimentLogs");
            migrationBuilder.DropColumn(name: "LoggedAt",  table: "ExperimentLogs");

            migrationBuilder.AddColumn<string>(
                name: "Log",
                table: "ExperimentLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Rollback SimulationSessions
            migrationBuilder.DropForeignKey(
                name: "FK_SimulationSessions_SimulationTemplates_TemplateId",
                table: "SimulationSessions");

            migrationBuilder.DropIndex(
                name: "IX_SimulationSessions_TemplateId",
                table: "SimulationSessions");

            migrationBuilder.DropColumn(name: "TemplateId", table: "SimulationSessions");
            migrationBuilder.DropColumn(name: "EndTime",    table: "SimulationSessions");
            migrationBuilder.DropColumn(name: "Status",     table: "SimulationSessions");

            // Rollback SimulationTemplates
            migrationBuilder.DropColumn(name: "SimulationName", table: "SimulationTemplates");
            migrationBuilder.DropColumn(name: "Description",    table: "SimulationTemplates");
        }
    }
}
