# Live Database Inventory

Read-only. Table names below are copy-pasted verbatim from `information_schema`,
no guessing. Full DDL detail in `live-schema.sql`.

## All 39 real tables in `public` (excluding `__EFMigrationsHistory`)

AiQuotaUsages, Announcements, AssignmentQuizDetails, AssignmentReportDetails,
AssignmentSimulationDetails, Assignments, AttendanceRecords, Classes,
ComponentDefinitions, ComponentGlueRegistry, ComponentSources, Courses,
Enrollments, FileEntity, GradeLevels, LabClassAssignments, LabProgresses, Labs,
Lessons, LoginHistories, Metrics, Modules, Notifications, PaymentPackages,
Payments, RefreshTokens, ResubmitRequests, Roles, Rubrics, Schedules, Schools,
SubmissionComments, Submissions, **Syllabuses**, TokenAccounts, TokenAllocations,
TokenTransactions, Users, VirtualLabProjects.

## Audit against the requested list

| Requested name | Live table | Notes |
|---|---|---|
| Syllabuses | `Syllabuses` ✅ | Id, Title, Description, ThumbnailUrl, GradeLevelId(FK), SubjectArea, Status, DisplayOrder, EstimatedHours, IsRequired, IsSystemOwned, CreatedAt, UpdatedAt. **No SchoolId, no IsStandard, no SourceSyllabusId.** |
| Courses | `Courses` ✅ | Id, Title, Description, SchoolId(FK, nullable), DisplayOrder, EstimatedHours, IsRequired, Status, SubjectArea, SyllabusId(FK, nullable), IsActive. |
| Modules | `Modules` ✅ | Id, CourseId(FK, NOT NULL), Title, Description, DisplayOrder, EstimatedMinutes, Input, Output. **No SyllabusId.** |
| Lessons | `Lessons` ✅ | Id, ModuleId(FK, NOT NULL), Title, Content, DisplayOrder, EstimatedMinutes, HasVirtualLab, Input, Output, LessonType, **LabId (FK→Labs, nullable)**. |
| Labs | `Labs` ✅ | Matches current `Lab.cs` field-for-field. **No LessonId column on Labs itself** — the FK direction is Lesson→Lab, not Lab→Lesson. |
| LabClassAssignments | `LabClassAssignments` ✅ | Id, LabId(FK), ClassId(FK), CreatedAt. Matches current `LabClassAssignment.cs` exactly. No LessonId (added then reverted — see drift report). |
| Schedules | `Schedules` ✅ | Id, ClassId(FK), StartTime, EndTime, **LessonId (FK→Lessons, nullable, UNIQUE)**. |
| Classes | `Classes` ✅ | Id, CourseId(FK), TeacherId(FK), StartDate, EndDate, ClassCode, SchoolId(FK), **GradeLevelId (FK→GradeLevels, NOT NULL)**. |
| Enrollments | `Enrollments` ✅ | Matches current `Enrollment` model (not shown here, unaffected). |
| Assignments | `Assignments` ✅ | Extended with AssignmentType/RubricId/ResubmitLimit/AllowResubmit — matches Resubmit workflow (protected subsystem), verified in sync. |
| Submissions | `Submissions` ✅ | Extended with AttemptNumber/AutoScore/FinalScore/ContentJson — matches Resubmit workflow, verified in sync. |
| Attendance | `AttendanceRecords` ✅ (no literal "Attendances" table) | ScheduleId(FK, nullable), unique (ScheduleId, StudentId) — matches current `AttendanceRecord.cs`, protected same-day rule unaffected. |
| Notifications | `Notifications` ✅ | Matches current entity. |
| Payments | `Payments` ✅ | Has OrderCode, PaymentLinkId, CheckoutUrl, Metadata — **deferred**, audit after DB recovery per instruction. |
| PaymentPackages | `PaymentPackages` ✅ | Has ExpiresAt, StudentLimit, DurationMonths — **deferred**. |
| ServicePackages | **NOT FOUND** | The ERD concept "Service Package" currently has no dedicated table; only `PaymentPackages` exists. Flagged for RED-2/Payment follow-up, not resolved here. |
| SystemLogs | **NOT FOUND** | RED-3 confirmed: no system-log table exists anywhere in the live DB. This is a real missing subsystem, not drift. |
| AuditLogs | **NOT FOUND** | Same as above — no audit-log table exists. |

## Bonus finding (not on the requested list, but directly relevant to RED-2)

`GradeLevels` is a real, populated-looking live table (Id, Name, Code,
DisplayOrder, Description, Level) referenced by both `Syllabuses.GradeLevelId`
and `Classes.GradeLevelId` — but **no `GradeLevel.cs` entity exists anywhere in
Git**. This is a fully missing entity, not a drifted one.
