using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureClassesTeacherIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Courses'
                          AND column_name = 'TeacherId'
                    ) THEN
                        ALTER TABLE "Courses" ADD COLUMN "TeacherId" integer;
                    END IF;

                    UPDATE "Courses" AS c
                    SET "TeacherId" = COALESCE(
                        (
                            SELECT u."Id"
                            FROM "Users" AS u
                            JOIN "Roles" AS r ON r."Id" = u."RoleId"
                            WHERE r."Name" = 'Teacher'
                              AND c."SchoolId" IS NOT DISTINCT FROM u."SchoolId"
                            ORDER BY u."Id"
                            LIMIT 1
                        ),
                        (
                            SELECT u."Id"
                            FROM "Users" AS u
                            JOIN "Roles" AS r ON r."Id" = u."RoleId"
                            WHERE r."Name" = 'Teacher'
                            ORDER BY u."Id"
                            LIMIT 1
                        )
                    )
                    WHERE c."TeacherId" IS NULL
                       OR NOT EXISTS (
                           SELECT 1
                           FROM "Users" AS existing_teacher
                           WHERE existing_teacher."Id" = c."TeacherId"
                       );

                    IF EXISTS (SELECT 1 FROM "Courses" WHERE "TeacherId" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot backfill Courses.TeacherId because no teacher user is available.';
                    END IF;

                    ALTER TABLE "Courses" ALTER COLUMN "TeacherId" SET NOT NULL;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Classes'
                          AND column_name = 'TeacherId'
                    ) THEN
                        ALTER TABLE "Classes" ADD COLUMN "TeacherId" integer;
                    END IF;

                    UPDATE "Classes" AS cls
                    SET "TeacherId" = COALESCE(
                        (
                            SELECT c."TeacherId"
                            FROM "Courses" AS c
                            WHERE c."Id" = cls."CourseId"
                            LIMIT 1
                        ),
                        (
                            SELECT u."Id"
                            FROM "Users" AS u
                            JOIN "Roles" AS r ON r."Id" = u."RoleId"
                            WHERE r."Name" = 'Teacher'
                              AND cls."SchoolId" IS NOT DISTINCT FROM u."SchoolId"
                            ORDER BY u."Id"
                            LIMIT 1
                        ),
                        (
                            SELECT u."Id"
                            FROM "Users" AS u
                            JOIN "Roles" AS r ON r."Id" = u."RoleId"
                            WHERE r."Name" = 'Teacher'
                            ORDER BY u."Id"
                            LIMIT 1
                        )
                    )
                    WHERE cls."TeacherId" IS NULL
                       OR NOT EXISTS (
                           SELECT 1
                           FROM "Users" AS existing_teacher
                           WHERE existing_teacher."Id" = cls."TeacherId"
                       );

                    IF EXISTS (SELECT 1 FROM "Classes" WHERE "TeacherId" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot backfill Classes.TeacherId because no teacher user is available.';
                    END IF;

                    ALTER TABLE "Classes" ALTER COLUMN "TeacherId" SET NOT NULL;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Courses_TeacherId"
                ON "Courses" ("TeacherId");
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_Courses_Users_TeacherId'
                    ) THEN
                        ALTER TABLE "Courses"
                        ADD CONSTRAINT "FK_Courses_Users_TeacherId"
                        FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id")
                        ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Classes_TeacherId"
                ON "Classes" ("TeacherId");
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_Classes_Users_TeacherId'
                    ) THEN
                        ALTER TABLE "Classes"
                        ADD CONSTRAINT "FK_Classes_Users_TeacherId"
                        FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id")
                        ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op. This migration repairs databases where the column
            // was missing despite the model snapshot already expecting it.
        }
    }
}
