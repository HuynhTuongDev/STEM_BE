# Syllabus / Course / Lesson / Lab Forensic Audit (STEP 8–10)

All answers read directly from live `information_schema` (see `live-schema.sql`
and `LIVE_DATABASE_INVENTORY.md`). No inference from migration names alone.

## STEP 8 — Syllabus forensic questions

1. **Syllabus có SchoolId không?** — **NO.** `Syllabuses` has no `SchoolId` column.
2. **Có IsStandard không?** — **NO.** No boolean flag of that name. The closest
   live concept is `IsSystemOwned` (boolean) — semantically similar ("owned by
   the platform" vs "owned by a school") but not the same field name/shape I had
   guessed in the earlier (reverted) attempt. Do not conflate the two.
3. **Có SourceSyllabusId không?** — **NO.** No self-referential FK exists on
   `Syllabuses`. There is no "derived from standard syllabus X" link at the
   database level today.
4. **Course liên kết Syllabus bằng gì?** — `Courses.SyllabusId` (nullable int,
   FK → `Syllabuses.Id`). A Course may or may not be attached to a Syllabus.
5. **Module liên kết Course hay Syllabus?** — **Course.** `Modules.CourseId`
   (NOT NULL, FK → `Courses.Id`). There is no `SyllabusId` on `Modules` despite
   the migration named `FixSyllabusCourseRelationship` — that migration fixed the
   Course↔Syllabus link, not Module↔Syllabus.
6. **Lesson liên kết Module như thế nào?** — `Lessons.ModuleId` (NOT NULL,
   FK → `Modules.Id`). Standard one-to-many, no surprises.
7. **Lab liên kết Lesson trực tiếp không?** — **Yes, but the FK points the
   opposite direction from what the question implies.** `Lessons.LabId`
   (nullable uuid, FK → `Labs.Id`). It is the Lesson that optionally points at
   one Lab, not the Lab pointing at a Lesson. `Labs` itself has no `LessonId`
   column.
8. **Schedule có LessonId không?** — **YES.** `Schedules.LessonId` (nullable
   int, FK → `Lessons.Id`), with a **unique index where not null**
   (`IX_Schedules_LessonId`). Meaning: a given Lesson can be scheduled at most
   once at a time.
9. **LabClassAssignment có LessonId không?** — **NO.** `LabClassAssignments`
   only has `LabId` + `ClassId`. A `LessonId` column was added
   (`AddLessonIdToLabClassAssignment`, 2026-08-20 16:55) and reverted 5 minutes
   later (`FixLabClassAssignmentLessonId`, 2026-08-20 17:01) — the live table
   today has no `LessonId`, and this matches the current Git entity exactly.
   Read as: whoever built this tried linking LabClassAssignment→Lesson directly
   and abandoned it in favor of reaching a Lesson's lab through
   `Lesson.LabId` instead.

## STEP 9 — COURSE_SCHEMA_EVOLUTION (Course = CRITICAL AREA)

Course accumulated 7 relevant untracked migrations across two clusters
(2026-07-01 and 2026-08-20/21). Live schema is the only reliable source; do not
trust migration names alone as a description of net effect.

| Field | Old inferred state (pre-drift Git) | Migration evidence | Current live state | Current Git state | Difference |
|---|---|---|---|---|---|
| `TeacherId` | absent | `AddCourseTeacherIdColumn` (2026-07-01) then never removed by name, but... | **absent** — no `TeacherId` column on `Courses` today | absent | Consistent: column was added then removed by some untracked step not named "RemoveCourseTeacherId" (possibly folded into `FixCoursesSchemaDrift` or `TestCurrentModel`). Net effect: no drift to fix here. |
| `SchoolId` | nullable int (matches Git) | `RemoveSchoolIdFromCourse` (2026-08-21 01:38) | **still present**, nullable, FK intact | nullable int (matches) | **Anomaly**: a migration literally named "remove SchoolId from Course" ran, but the column is still live. Per the pipeline's own rule (live DB is ground truth over migration *names*), treat `SchoolId` as still real and keep it. Do not attempt to drop it — that would be inventing a change the actual database does not reflect. |
| `SyllabusId` | absent | `FixSyllabusCourseRelationship` (2026-08-20 11:45) | nullable int, FK → Syllabuses | absent | Git is missing this column — must be added during reconstruction. |
| `DisplayOrder`, `EstimatedHours`, `IsRequired`, `Status`, `SubjectArea`, `IsActive` | absent | Introduced somewhere across the 2026-08-18/19/20 "sync" cluster (`SyncDbSchema`, `FinalizeSchemaSync`, or similar) — exact origin migration for each individual column cannot be attributed without the missing `.cs` files | present live, with real defaults (`Status` defaults to `''`, `IsRequired`/`IsActive` default `false`, etc.) | absent | Git is missing all 6 columns — must be added during reconstruction, using the exact live defaults/nullability. |
| `Id` identity | int identity (matches Git) | `FixCourseIdIdentity` (2026-07-01 04:58) | int, identity-by-default (matches) | int identity | No net drift — whatever this migration fixed, the end state matches current Git already. |

**Conclusion**: Course requires reconstruction of 7 columns
(`SyllabusId`, `DisplayOrder`, `EstimatedHours`, `IsRequired`, `Status`,
`SubjectArea`, `IsActive`) plus one FK (→ Syllabuses). `SchoolId` stays exactly
as-is despite the misleading migration name. No column should be dropped from
`Course.cs` — everything currently in Git also exists live.

## STEP 10 — Lesson/Lab/Schedule/LabClassAssignment audit

| Entity | Migration(s) | Real target confirmed live |
|---|---|---|
| `Modules`/`Lessons` | `AddInputOutputFields` (2026-08-20 11:11) | `Input`/`Output` text columns added to **both** `Modules` and `Lessons` (likely for a "learning objective in / outcome out" pedagogy field, not a technical I/O concept — naming is generic, actual business meaning should be confirmed with the user/team before UI copy is written, but the columns themselves are unambiguous: nullable-safe `text NOT NULL DEFAULT ''`). |
| `Lessons` | `FixLessonRelationships` (2026-08-20 11:37) | Net effect: `Lessons.LabId` FK → `Labs` exists live today. This is very likely what this migration established. |
| `LabClassAssignments` | `AddLessonIdToLabClassAssignment` + `FixLabClassAssignmentLessonId` (2026-08-20, 5 min apart) | Net effect is a no-op relative to Git — column added then removed, current live table matches current Git exactly. No reconstruction needed here. |
| `Schedules` | `AddLessonIdToSchedule` (2026-08-21 12:00, **the very last migration ever applied**) | `Schedules.LessonId` FK → `Lessons`, unique-when-not-null. This is the newest live change and the most likely "still in flux" one — flagged accordingly in the reconstruction plan. |

Overall shape confirmed: **Syllabus → Course (optional) → Module → Lesson →
(optional 1:1) Lab**, with **Schedule → Lesson (optional, unique)** as a second,
independent link used for the calendar/attendance side, and
**LabClassAssignment → Lab + Class directly** (bypassing Lesson) for whole-class
lab assignment regardless of any one lesson's schedule.
