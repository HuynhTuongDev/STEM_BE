# Three-Way Diff — Current Source Model vs Model Snapshot vs Live Database

## Axis 1: Current Source Model vs Model Snapshot

**No drift.** `StemDbContextModelSnapshot.cs` is a generated file kept in sync with
the entity classes + Fluent config by EF tooling. Spot-checked `Course`, `Module`,
`Class`, `Schedule`, `Material`/`Course.File` — the snapshot mirrors the current
source model exactly (same properties, same FK/navigation wiring, same
`OnDelete` behaviors). `dotnet build` produces zero pending-model-changes warnings,
confirming this axis is consistent by construction. This is expected: nothing
edits the snapshot by hand, only `dotnet ef migrations add` regenerates it
alongside the model.

Because axis 1 has zero drift, the two three-way legs collapse into one
meaningful comparison:

## Axis 2: (Source Model + Snapshot) vs Live Database — the real drift

This is a restatement of `CURRENT_EF_MODEL.md`'s findings, framed as the actual
three-way diff outcome:

| Domain | Source+Snapshot say | Live DB says | Verdict |
|---|---|---|---|
| Course | Id, Title, Description, SchoolId? | + SyllabusId, DisplayOrder, EstimatedHours, IsRequired, Status, SubjectArea, IsActive | Source is missing 6 live columns |
| Module | Id, CourseId, Title | + Description, DisplayOrder, EstimatedMinutes, Input, Output | Source is missing 5 live columns |
| Lesson | Id, ModuleId, Title, Content | + DisplayOrder, EstimatedMinutes, HasVirtualLab, Input, Output, LessonType, LabId | Source is missing 7 live columns |
| Schedule | Id, ClassId, StartTime, EndTime | + LessonId | Source is missing 1 live column |
| Class | Id, ClassCode, SchoolId, CourseId, TeacherId, dates | + GradeLevelId | Source is missing 1 live column, and the referenced `GradeLevel` entity doesn't exist at all |
| Syllabus | entity does not exist | full table exists (9 business columns + FK to GradeLevels) | Source is missing the entire entity |
| GradeLevel | entity does not exist | full table exists | Source is missing the entire entity |
| Material / Course.File | entity + DbSet + Fluent config exist, map to `Materials`/`Files` | **tables do not exist** | Source has 2 dead entities pointing at dropped tables |
| Payment / PaymentPackage / TokenAccount / TokenAllocation / TokenTransaction | entities do not exist | full tables exist | Deferred — audit only, no reconstruction this milestone |
| Lab, LabClassAssignment, School, AttendanceRecord, Announcement, Enrollment, Resubmit workflow entities, Component Registry entities | match | match | ✅ no drift |

## Conclusion feeding STEP 7

The live database is internally coherent (every FK target exists, every type is
sane, no orphaned constraints found). All drift is one-directional: **the live DB
is strictly ahead of the source model** for Course/Module/Lesson/Schedule/Class/
Syllabus/GradeLevel, plus two small pockets of source-ahead-of-DB dead code
(Material/File) that must be deleted, not migrated.

This supports **CASE B**: source lost, but the live DB is unambiguous enough to
reconstruct source from directly.
