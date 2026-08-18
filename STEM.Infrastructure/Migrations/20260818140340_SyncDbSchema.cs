using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncDbSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints first
            migrationBuilder.Sql(@"
                ALTER TABLE ""QuizAttemptAnswers"" DROP CONSTRAINT IF EXISTS ""FK_QuizAttemptAnswers_QuizAttempts_QuizAttemptId"";
                ALTER TABLE ""QuizAttemptAnswers"" DROP CONSTRAINT IF EXISTS ""FK_QuizAttemptAnswers_QuizAnswers_AnswerId"";
                ALTER TABLE ""QuizAttemptAnswers"" DROP CONSTRAINT IF EXISTS ""FK_QuizAttemptAnswers_QuizQuestions_QuestionId"";
                ALTER TABLE ""QuizAttempts"" DROP CONSTRAINT IF EXISTS ""FK_QuizAttempts_Quizzes_QuizId"";
                ALTER TABLE ""QuizAttempts"" DROP CONSTRAINT IF EXISTS ""FK_QuizAttempts_Users_StudentId"";
                ALTER TABLE ""SimulationTemplates"" DROP CONSTRAINT IF EXISTS ""FK_SimulationTemplates_Simulations_SimulationId"";
                ALTER TABLE ""ExperimentLogs"" DROP CONSTRAINT IF EXISTS ""FK_ExperimentLogs_SimulationSessions_SessionId"";
                ALTER TABLE ""LiveMonitorings"" DROP CONSTRAINT IF EXISTS ""FK_LiveMonitorings_SimulationSessions_SessionId"";
                ALTER TABLE ""LiveMonitorings"" DROP CONSTRAINT IF EXISTS ""FK_LiveMonitorings_Users_TeacherId"";
            ");

            // Drop tables that exist in DB but not in code
            migrationBuilder.DropTable(name: "QuizAttemptAnswers");
            migrationBuilder.DropTable(name: "QuizAttempts");
            migrationBuilder.DropTable(name: "Simulations");
            migrationBuilder.DropTable(name: "ExperimentLogs");
            migrationBuilder.DropTable(name: "LiveMonitorings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
