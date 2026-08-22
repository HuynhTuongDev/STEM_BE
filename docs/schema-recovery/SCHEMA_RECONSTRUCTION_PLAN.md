# Schema Reconstruction Plan (STEP 12)

**Principle, restated from the instruction (do not violate):**
Goal is `SOURCE CODE → MATCH → LIVE DATABASE`. Never the reverse. No
`CREATE TABLE` for anything that already exists live. No destructive migration.
No `dotnet ef database update` in this phase at all.

## STEP 6 result (for the record)

Exhaustively searched for the missing source:
- `git log --all` / `git reflog --all` on `STEM_BE` — no commit anywhere touches
  `STEM.Core/Entities/Courses/Syllabus.cs` or the 28 orphaned migration names.
- All 51 local + remote branches individually checked via
  `git show <branch>:STEM.Core/Entities/Courses/Syllabus.cs` — zero hits.
- `git fsck --no-reflog --unreachable --dangling` — found unreachable
  blobs/trees (mostly from this session's own earlier reverted attempt), scanned
  every dangling blob's content for `class Syllabus`, `IsSystemOwned`,
  `SubjectArea` — zero matches.
- No `backup`/`archive`/`publish`/`old`/`99_ARCHIVE` directories exist in the
  repo (checked via `Glob`).

**Verdict: CASE B.** The source is not lost-and-findable — it was applied to the
shared database via `dotnet ef database update` and never committed anywhere.
Live DB is the only remaining source of truth, and it is internally coherent
enough (no contradictions, every FK resolves) to reconstruct from directly.

## Entities to add (new files)

1. **`STEM.Core/Entities/Courses/GradeLevel.cs`** (new) — `Id`, `Name`, `Code`,
   `DisplayOrder`, `Description`, `Level`, `CreatedAt`, `UpdatedAt`. Needed first
   because both Syllabus and Class FK into it.
2. **`STEM.Core/Entities/Courses/Syllabus.cs`** (new) — `Id`, `Title`,
   `Description`, `ThumbnailUrl?`, `GradeLevelId?`, `SubjectArea`, `Status`,
   `DisplayOrder`, `EstimatedHours`, `IsRequired`, `IsSystemOwned`, `CreatedAt`,
   `UpdatedAt`, nav `GradeLevel?`, nav `Courses` collection. **No `SchoolId`, no
   `SourceSyllabusId` — confirmed absent live, do not invent them.**

## Entities to extend (existing files, additive only)

3. **`Course.cs`** — add `SyllabusId?`, `DisplayOrder`, `EstimatedHours`,
   `IsRequired`, `Status`, `SubjectArea`, `IsActive`, nav `Syllabus?`. Keep
   `SchoolId` unchanged (live anomaly resolved in favor of keeping it).
4. **`Module.cs`** — add `Description`, `DisplayOrder`, `EstimatedMinutes`,
   `Input`, `Output`. No `SyllabusId` (confirmed Module stays Course-scoped).
5. **`Lesson.cs`** — add `DisplayOrder`, `EstimatedMinutes`, `HasVirtualLab`,
   `Input`, `Output`, `LessonType`, `LabId?`, nav `Lab?`.
6. **`Schedule.cs`** — add `LessonId?`, nav `Lesson?` (unique constraint on the
   FK, matching live `IX_Schedules_LessonId`).
7. **`Class.cs`** — add `GradeLevelId`, nav `GradeLevel?`.

## Entities to remove (dead code, matches a real live drop)

8. **`Material.cs`**, DbSet `Materials`, and its Fluent config — table dropped
   live, nothing in code queries it (verified via grep).
9. **`Courses/File.cs`** (`STEM.Core.Entities.Courses.File`), DbSet `Files`, and
   its Fluent config — same drop event (`RemoveUnusedMaterialsAndFiles`).
   Careful: do **not** confuse this with `SubmissionFile` (`DbSet<SubmissionFile>
   FileEntity`), which is a different entity mapped to the still-live
   `FileEntity` table — that one stays untouched.

## Explicitly NOT in scope for this reconstruction pass

- `LabClassAssignment` — already matches live exactly, no change.
- `Lab` itself — protected Virtual Lab subsystem, untouched.
- `Payment`, `PaymentPackage`, `TokenAccount`, `TokenAllocation`,
  `TokenTransaction` — deferred per instruction, audit only after this recovery
  completes.
- `SystemLogs`/`AuditLogs` — do not exist live at all; this is RED-3, a fresh
  design task, not a reconstruction task. Not touched here.
- `ServicePackages` — does not exist live under that name; deferred with Payment.

## Fluent API / DbContext changes required

- New `DbSet<GradeLevel> GradeLevels`, `DbSet<Syllabus> Syllabuses`.
- New Fluent blocks: `Syllabus → GradeLevel` (SetNull, since nullable FK),
  `Course → Syllabus` (SetNull), `Module → Course` (unchanged, Cascade already
  exists), `Lesson → Lab` (SetNull — a Lesson losing its Lab shouldn't cascade
  delete the Lab), `Schedule → Lesson` (SetNull + unique index), `Class →
  GradeLevel` (Restrict, matching the existing `Class → School`/`Class →
  Course` Restrict pattern already in the model).
- Remove the `Material`/`Courses.File` Fluent blocks and DbSets entirely.

## Execution order (STEP 13–17, mirrors the mandated pipeline)

1. Add `GradeLevel.cs`, `Syllabus.cs`.
2. Extend `Course.cs`, `Module.cs`, `Lesson.cs`, `Schedule.cs`, `Class.cs`.
3. Remove `Material.cs`, `Courses/File.cs`, and their DbContext registrations.
4. Update `StemDbContext.cs`: new DbSets, new/removed Fluent blocks.
5. Fix any compile fallout the same way `GetStudentLearningProgressHandler.cs`
   needed a fix last time `Module.CourseId` changed shape (that specific
   nullable-CourseId change is **not** part of this plan — `CourseId` stays
   `NOT NULL`, matching live — so that particular fallout should not recur, but
   any other caller touching the extended entities must still be checked).
6. `dotnet build` + `dotnet test` — must return to 0 errors / 107 passing before
   proceeding. **No `dotnet ef database update` at any point in this phase.**
7. Generate one temporary migration (`dotnet ef migrations add
   TEMP_ReconcileWithLiveSchema`) purely to verify the model now matches the
   live DB shape. Inspect its `Up()`: it must be empty or near-empty (only
   trivial things like model-snapshot metadata). Any `CreateTable` for an
   existing table, `DropColumn`, `DropForeignKey`, or unexpected rename means
   the reconstructed model is wrong — fix the model, not the migration.
   **Do not run `database update`. Delete the temp migration once verified**
   (`dotnet ef migrations remove`), matching how the earlier aborted Phase 1
   attempt was safely undone.
8. Commit the reconciled model alone: `chore(database): reconcile EF model with
   live schema`. No push.

Only after Checkpoint 2 passes does RED-2 (Standard Syllabus business rules —
adopt/derive workflow, Standard vs School vs Course syllabus semantics) begin,
building *on top of* this now-accurate baseline rather than guessing at it.
