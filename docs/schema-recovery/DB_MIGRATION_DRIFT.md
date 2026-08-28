# DB Migration Drift — Three-Way Classification

Generated: 2026-08-22. Read-only audit. No DB writes performed.

Source of DB truth: `__EFMigrationsHistory` on the shared Supabase Postgres instance
(`aws-1-ap-southeast-1.pooler.supabase.com`, queried directly via `pg`, read-only).
Source of Git truth: `STEM.Infrastructure/Migrations/*.cs` on `Trieu/Vitural_lab_v2`
**plus an exhaustive search across all 51 local + remote branches** (none of the
missing files exist on any branch — see `SCHEMA_RECONSTRUCTION_PLAN.md` STEP 6).

Totals: **66 in DB, 39 in Git, 37 in both.**

## A. IN_DB_AND_GIT (37) — no drift

All pre-2026-05-26 through `AddComponentRegistry` (2026-08-19) except the two
gaps below match exactly by MigrationId. This is the trusted baseline. No action needed.

## B. IN_DB_NOT_IN_GIT (29) — the real drift

These migrations physically ran against the shared database. Their `.cs`/`.Designer.cs`
source does not exist anywhere in Git history (confirmed via `git log --all`,
`git reflog --all`, and a scripted search of all 51 branches — zero hits). The only
way to know what they did is to read the live schema directly (done — see
`LIVE_DATABASE_INVENTORY.md` and `CURRENT_EF_MODEL.md` for the resulting diff).

| Migration | Likely affected domain | Snapshot relevance | Risk |
|---|---|---|---|
| 20260526083925_CleanupUnusedEntities | general cleanup | none — predates any tracked entity referencing removed things | LOW |
| 20260526084936_RemoveInvitations | Invitations (already absent from current model) | none | LOW |
| 20260624040152_AddStudentLearningApis | Student learning / progress APIs | possible — `GetStudentLearningProgressHandler` exists in current code | MEDIUM |
| 20260624090000_AddAttendanceTracking | Attendance (**protected: same-day rule**) | `AttendanceRecords` matches current `AttendanceRecord.cs` exactly | LOW (verified in sync) |
| 20260701033016_ApplyClassBasedQuizAndVirtualLab | Course/Class/VirtualLab | Course = CRITICAL AREA (see STEP 9) | HIGH |
| 20260701042459_RemoveTeacherIdFromCourse | Course | Course = CRITICAL AREA | HIGH |
| 20260701044828_TestCurrentModel | unknown — exploratory name | Course = CRITICAL AREA (same timestamp cluster) | HIGH |
| 20260701045824_FixCourseIdIdentity | Course PK/identity | Course = CRITICAL AREA | HIGH |
| 20260701124113_AddCourseTeacherIdColumn | Course | superseded later? live Course has no TeacherId column today | HIGH |
| 20260715084800_FixCoursesSchemaDrift | Course | named "schema drift" — literally this problem, one cycle earlier | HIGH |
| 20260818140340_SyncDbSchema | general | exploratory/sync name | MEDIUM |
| 20260818150859_FinalizeSchemaSync | general | exploratory/sync name | MEDIUM |
| 20260818192143_AddAnnouncementsTable | Announcements | `Announcements` table matches current `Announcement.cs` + `Class.Announcements` nav | LOW (verified in sync) |
| 20260819072954_EmptyMigration | none (literally empty per name) | none | LOW |
| 20260819092558_AddPaymentEntities | Payment domain — **deferred per user instruction** | Payments/PaymentPackages exist live, only partially modeled in Git | DEFERRED |
| 20260819112629_AddOrderCodeToPayment | Payment domain — **deferred** | `Payments.OrderCode` live, not in any local Payment entity file found | DEFERRED |
| 20260819124232_AddExpiresAtToPaymentPackage | Payment domain — **deferred** | `PaymentPackages.ExpiresAt` live | DEFERRED |
| 20260819124605_AddStudentLimitToPaymentPackage | Payment domain — **deferred** | `PaymentPackages.StudentLimit` live | DEFERRED |
| 20260819154233_AddUpdatedAtToTokenAllocation | Payment/Token domain — **deferred** | `TokenAllocations.UpdatedAt` live | DEFERRED |
| 20260820111102_AddInputOutputFields | Module/Lesson | `Modules.Input/Output`, `Lessons.Input/Output` live, absent from current `Module.cs`/`Lesson.cs` | HIGH (RED-2 core) |
| 20260820111746_RemoveUnusedMaterialsAndFiles | Course/Material | `Materials` table **dropped live**; `Material.cs` + `DbSet<Material>` + Fluent config **still exist in Git** — active drift | HIGH |
| 20260820113706_FixLessonRelationships | Lesson/Module | RED-2 core | CRITICAL |
| 20260820114508_FixSyllabusCourseRelationship | Syllabus/Course | RED-2 core | CRITICAL |
| 20260820115343_AddGradeLevelIdToClass | Class/GradeLevel | `Classes.GradeLevelId` live, no `GradeLevel` entity in Git at all | CRITICAL |
| 20260820165551_AddLessonIdToLabClassAssignment | LabClassAssignment | superseded 5 min later, see next row | LOW (self-resolved) |
| 20260820170111_FixLabClassAssignmentLessonId | LabClassAssignment | **reverted** the column added above — live `LabClassAssignments` has no `LessonId`, matches current Git model exactly | LOW (in sync) |
| 20260821013810_RemoveSchoolIdFromCourse | Course | **anomaly**: live `Courses.SchoolId` still exists (nullable) despite this migration name — see `COURSE_SCHEMA_EVOLUTION` in `ERD_LIVE_DB_MAPPING.md` | HIGH |
| 20260821110259_RemoveSyllabusDistribution | Syllabus | RED-2 core — likely removed a `SchoolId`/distribution concept from Syllabus; live `Syllabuses` has no `SchoolId` today, consistent with the name | CRITICAL |
| 20260821120031_AddLessonIdToSchedule | Schedule/Lesson | RED-2 core; `Schedules.LessonId` live (nullable, unique), absent from current `Schedule.cs` | CRITICAL |

## C. IN_GIT_NOT_IN_DB (2) — Git ahead of DB

| Migration | Effect (read from source) | Risk |
|---|---|---|
| 20260726124256_AddRobotDeliveryKitComponentGlueRegistry | Pure `InsertData` seeding 10 rows into `ComponentGlueRegistry` (robot-kit component seed data). Never applied — live `ComponentGlueRegistry` is missing these 10 rows. | LOW — data-only, non-destructive, additive |
| 20260809100000_AddAiQuotaUsage | `CreateTable "AiQuotaUsage"` (singular). **Dead/superseded**: a later migration `20260812120524_AddAiQuotaUsagesTable` (plural, applied, in category A) created the real table. This file was never applied and appears to be an abandoned draft. | LOW — orphaned migration file, harmless, candidate for cleanup (not touched in this milestone) |

## Verdict feeding STEP 7

No internal contradiction was found in the live schema itself (every FK resolves,
every column has a coherent type). The problem is 100% "source code lost", not
"database inconsistent". This supports **CASE B** (source lost, but live DB is
clear enough to reconstruct from) — see `ERD_LIVE_DB_MAPPING.md` and
`SCHEMA_RECONSTRUCTION_PLAN.md`.
