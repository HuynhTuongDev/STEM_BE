using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tables that don't exist in current schema - skip
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Certificates') THEN
                        DROP TABLE ""Certificates"";
                    END IF;
                END $$;
            ");
            
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Feedbacks') THEN
                        DROP TABLE ""Feedbacks"";
                    END IF;
                END $$;
            ");
            
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Grades') THEN
                        DROP TABLE ""Grades"";
                    END IF;
                END $$;
            ");
            
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Leaderboards') THEN
                        DROP TABLE ""Leaderboards"";
                    END IF;
                END $$;
            ");
            
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM pg_tables WHERE tablename = 'Simulations') THEN
                        DROP TABLE ""SimulationTemplates"";
                        DROP TABLE ""ExperimentLogs"";
                        DROP TABLE ""LiveMonitorings"";
                        DROP TABLE ""SimulationSessions"";
                        DROP TABLE ""Simulations"";
                    END IF;
                END $$;
            ");
            
            // Only modify Rubrics if columns exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'Rubrics' AND column_name = 'Description'
                    ) THEN
                        ALTER TABLE ""Rubrics"" DROP COLUMN ""Description"";
                    END IF;
                    IF EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'Rubrics' AND column_name = 'Name'
                    ) THEN
                        ALTER TABLE ""Rubrics"" DROP COLUMN ""Name"";
                    END IF;
                END $$;
            ");
            
            // Add columns if they don't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'Rubrics' AND column_name = 'AssignmentId'
                    ) THEN
                        ALTER TABLE ""Rubrics"" ADD COLUMN ""AssignmentId"" integer NOT NULL DEFAULT 0;
                    END IF;
                    IF NOT EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'Rubrics' AND column_name = 'MaxScore'
                    ) THEN
                        ALTER TABLE ""Rubrics"" ADD COLUMN ""MaxScore"" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            ");
            
            // Add FK only if not exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Rubrics_Assignments_AssignmentId'
                    ) THEN
                        ALTER TABLE ""Rubrics"" ADD CONSTRAINT ""FK_Rubrics_Assignments_AssignmentId"" 
                        FOREIGN KEY (""AssignmentId"") REFERENCES ""Assignments""(""Id"") ON DELETE CASCADE;
                    END IF;
                END $$;
            ");
            
            // Add index only if not exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM pg_indexes WHERE tablename = 'Rubrics' AND indexname = 'IX_Rubrics_AssignmentId'
                    ) THEN
                        CREATE INDEX ""IX_Rubrics_AssignmentId"" ON ""Rubrics"" (""AssignmentId"");
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Rubrics_AssignmentId", table: "Rubrics");
            migrationBuilder.DropColumn(name: "AssignmentId", table: "Rubrics");
            migrationBuilder.DropColumn(name: "MaxScore", table: "Rubrics");
        }
    }
}
