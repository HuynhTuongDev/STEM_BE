# ERD → Live DB → Source Mapping (STEP 11)

| ERD Concept | Live DB | Current Source | Recovered Source | Final Decision |
|---|---|---|---|---|
| Master Admin | `Users` + `Roles` (role-based, no separate table) | `User`, `Role` entities exist | n/a — not lost | Reuse as-is. No dedicated "Master Admin" table; it's a `User` with an admin `Role`. |
| School | `Schools` | `School.cs` ✅ matches live exactly | n/a | Reuse as-is. |
| School Admin | `Users` scoped to a `School` via role + `SchoolId` on user (not audited here — outside RED-1/RED-2/RED-3 scope) | existing | n/a | Out of scope this milestone. |
| Standard Syllabus | `Syllabuses` (GradeLevelId, SubjectArea, Status, DisplayOrder, EstimatedHours, IsRequired, **IsSystemOwned**, no SchoolId, no SourceSyllabusId) | **missing entirely** | Not found anywhere in Git (51 branches + reflog + fsck dangling objects — zero hits) | **CASE B**: reconstruct `Syllabus.cs` to match the live table exactly. `IsSystemOwned=true` is the live mechanism for "this is a Master-Admin-owned standard syllabus" — do not invent `IsStandard`/`SourceSyllabusId`, they don't exist. |
| Module | `Modules` (+Description, DisplayOrder, EstimatedMinutes, Input, Output) | `Module.cs` — missing 5 columns | Reconstruct from live schema | Extend existing `Module.cs`, keep `CourseId` as the only parent FK (no `SyllabusId` — confirmed absent live). |
| Lesson | `Lessons` (+DisplayOrder, EstimatedMinutes, HasVirtualLab, Input, Output, LessonType, LabId) | `Lesson.cs` — missing 7 columns | Reconstruct from live schema | Extend existing `Lesson.cs`, add `LabId` (nullable FK → `Lab`). |
| Lab | `Labs` | `Lab.cs` ✅ matches exactly | n/a | Reuse as-is — **do not touch, Virtual Lab is a protected subsystem.** Only the *pointer into* Lab (`Lesson.LabId`) is new; `Lab` itself is unchanged. |
| Course | `Courses` (+SyllabusId, DisplayOrder, EstimatedHours, IsRequired, Status, SubjectArea, IsActive) | `Course.cs` — missing 7 fields (see `COURSE_SCHEMA_EVOLUTION`) | Reconstruct from live schema | Extend existing `Course.cs`. Keep `SchoolId` (live anomaly resolved in favor of live truth — see forensic audit). |
| Course Class | `Classes` (this IS "Course Class" — one Class is one running instance of one Course) | `Class.cs` — missing `GradeLevelId` | Reconstruct + also need new `GradeLevel` entity | Extend `Class.cs` with `GradeLevelId` FK. |
| Class | same table as above — the business ERD's "Course Class" and "Class" collapse into the single live `Classes` table; there is no second, separate roster-only "Class" concept in the live schema | same | same | No new entity — one live table already covers both ERD boxes. Flag this ERD/implementation naming collapse explicitly rather than inventing a second table. |
| Student / Teacher | `Users` (role-based) | existing | n/a | Reuse as-is. |
| Enrollment | `Enrollments` | `Enrollment.cs` — assumed in sync (not part of this audit's column-level check, no red flag found) | n/a | Reuse as-is. |
| Schedule | `Schedules` (+LessonId, unique) | `Schedule.cs` — missing `LessonId` | Reconstruct | Extend `Schedule.cs` with nullable `LessonId` FK, unique constraint. |
| Assignment | `Assignments` | matches (Resubmit workflow, protected) | n/a | Reuse as-is, do not touch. |
| Submission | `Submissions` | matches (Resubmit workflow, protected) | n/a | Reuse as-is, do not touch. |
| Attendance | `AttendanceRecords` | matches (same-day rule, protected) | n/a | Reuse as-is, do not touch. |
| Material | `Materials`/`Files` tables **dropped live** | `Material.cs`/`Courses.File` still registered — **dead code** | n/a — nothing to recover, this direction is deletion not reconstruction | Remove `Material`/`Courses.File` entities, their DbSets, and their Fluent config to make source match live reality. |
| Notification | `Notifications` | matches | n/a | Reuse as-is. |
| System Log | **no table exists live** | no entity exists | n/a — this is RED-3, a real gap, not drift | New subsystem to design fresh in a later phase. Confirmed genuinely absent, not lost. |
| Service Package | **no `ServicePackages` table** — only `PaymentPackages` exists | no dedicated entity | n/a | Deferred — Payment/Service-Package domain audit happens only after this recovery completes, per instruction. Do not design this now. |

## Also discovered, not on the original ERD list but load-bearing for it

| Concept | Live DB | Current Source | Decision |
|---|---|---|---|
| GradeLevel | `GradeLevels` (Name, Code, DisplayOrder, Description, Level) | **entity does not exist at all** | Reconstruct a minimal `GradeLevel` entity — required because both `Syllabus.GradeLevelId` and `Class.GradeLevelId` FK into it; cannot reconstruct Syllabus/Class correctly without it. |
