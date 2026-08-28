# Current EF Model Audit (Git, `Trieu/Vitural_lab_v2`)

Source: `STEM.Infrastructure/Data/StemDbContext.cs` DbSets + Fluent config,
entity classes under `STEM.Core/Entities/**`.

30 DbSets registered. Relevant entities for RED-1/RED-2, current source location,
and status against the live DB:

| Entity | Table (DbSet/ToTable) | PK | Key columns (Git) | Live DB status |
|---|---|---|---|---|
| `Course` (`Courses/Course.cs`) | Courses | Id | Title, Description, SchoolId? | **Missing live columns**: SyllabusId, DisplayOrder, EstimatedHours, IsRequired, Status, SubjectArea, IsActive |
| `Module` (`Courses/Module.cs`) | Modules | Id | CourseId, Title | **Missing live columns**: Description, DisplayOrder, EstimatedMinutes, Input, Output |
| `Lesson` (`Courses/Lesson.cs`) | Lessons | Id | ModuleId, Title, Content | **Missing live columns**: DisplayOrder, EstimatedMinutes, HasVirtualLab, Input, Output, LessonType, LabId |
| `Lab` (`Simulations/Lab.cs`) | Labs | Id | (full field list) | ✅ matches live exactly |
| `LabClassAssignment` (`Simulations/LabClassAssignment.cs`) | LabClassAssignments | Id | LabId, ClassId, CreatedAt | ✅ matches live exactly (LessonId add+revert already settled) |
| `Schedule` (`Classes/Schedule.cs`) | Schedules | Id | ClassId, StartTime, EndTime | **Missing live column**: LessonId |
| `Class` (`Classes/Class.cs`) | Classes | Id | ClassCode, SchoolId, CourseId, TeacherId, dates | **Missing live column**: GradeLevelId |
| `School` (`Schools/School.cs`) | Schools | Id | (full field list) | ✅ matches live exactly |
| `Material` (`Courses/Material.cs`) | Materials (ToTable) | — | — | ❌ **Table dropped live. Entity + DbSet + Fluent config still active in Git — dead reference.** |
| `STEM.Core.Entities.Courses.File` | Files (ToTable) | — | — | ❌ **Table dropped live. Same drop event as Material (`RemoveUnusedMaterialsAndFiles`).** |
| — (no entity) | — | — | — | ❌ **`Syllabus` entity does not exist in Git at all.** Live `Syllabuses` table is fully built (see inventory). |
| — (no entity) | — | — | — | ❌ **`GradeLevel` entity does not exist in Git at all.** Live `GradeLevels` table is fully built and referenced by both Syllabuses and Classes. |
| — (no entity) | — | — | — | ⏸ **`Payment`, `PaymentPackage`, `TokenAccount`, `TokenAllocation`, `TokenTransaction`** entities do not exist in Git. Live tables exist and are fully built. Deferred per instruction — audit only, no reconstruction in this milestone. |

## Verified in-sync entities (protected subsystems — confirmed untouched by drift)

`AttendanceRecord`, `Assignment`/`Submission`/`ResubmitRequest`/`Rubric`/`SubmissionComment`
(Resubmit workflow), `ComponentDefinition`/`ComponentSource`/`ComponentGlueRegistry`
(Component Registry), `Announcement`, `Enrollment`, `VirtualLabProject`, `LabProgress`,
`AiQuotaUsage` all match their live tables column-for-column. No action needed on these.

## Dead code found (not previously known)

`Material` and `Courses.File` are two live entities in the current git model that
point at tables no longer in the database. No production code queries either
`DbSet` directly (verified via grep — zero references outside `StemDbContext.cs`
itself), so this is dormant, not an active runtime crash. It must be removed as
part of STEP 13/14 reconstruction to make `EF MODEL == LIVE DATABASE`.
