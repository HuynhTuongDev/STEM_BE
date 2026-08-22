# ERD Implementation Mapping — Standard Syllabus Domain (RED-2)

## STEP 18 — Recovered domain audit

| Concept | Entity | Repository | UseCase | API | FE | Missing |
|---|---|---|---|---|---|---|
| Syllabus | `Syllabus.cs` ✅ (recovered, commit b06674a) | none | none | none | none | Everything above entity — repository, handlers, DTOs, controller, FE. |
| GradeLevel | `GradeLevel.cs` ✅ (recovered) | none | none | none | none | Same — entity only. |
| Course | `Course.cs` ✅ | `ICourseRepository`/`CourseRepository` ✅ | Create/Update/Delete/GetDetail/GetList ✅ | `CoursesController` ✅ | `schoolAdminApi.coursesApi`, `CoursesPage.tsx` ✅ | DTO/handlers don't expose `SyllabusId`/`DisplayOrder`/`EstimatedHours`/`IsRequired`/`Status`/`SubjectArea`/`IsActive` yet — out of RED-2's required scope (STEP 25 only says "preserve", not "expose"), noted as a gap for later. |
| Module | `Module.cs` ✅ | none | none | none | none | Full application layer missing. |
| Lesson | `Lesson.cs` ✅ | none | none | none | none | Full application layer missing. |
| Lab | `Lab.cs` ✅ (protected) | `ILabService` etc. ✅ | full Virtual Lab stack ✅ | existing Lab controllers ✅ | existing Virtual Lab UI ✅ | Nothing — Lab itself is untouched, only its *pointer from* Lesson is new. |
| Schedule | `Schedule.cs` ✅ | `IScheduleRepository`/`ScheduleRepository` ✅ | Create/Update/Delete/GetTeacherSchedule/GetStudentSchedule ✅ | (schedules controller, not audited in detail — out of scope) | existing | `LessonId` not yet exposed by handlers — out of RED-2 scope. |
| Class | `Class.cs` ✅ | `IClassRepository` ✅ | full CRUD ✅ | `ClassesController` (implied) ✅ | existing | `GradeLevelId` not yet exposed by handlers — out of RED-2 scope. |

**Empirical finding (read-only query, no schema change):** the live DB already
contains **one real, hand-built Syllabus** (`Id=1`, "Chương trinh khối 12"),
**one real GradeLevel** ("Khối 12" / Level 12), **one Course** linked to it via
`SyllabusId=1` with `SchoolId=NULL`, **7 Modules**, and **16 Lessons** — a fully
populated real curriculum tree that predates any application code. This is
concrete field evidence, not just column-name inference, for every decision
below.

## STEP 19 — Canonical hierarchy (decision)

```
Syllabus (System-owned)
   ↓ (Course.SyllabusId, nullable)
Course
   ↓ (Module.CourseId, NOT NULL)
Module
   ↓ (Lesson.ModuleId, NOT NULL)
Lesson
   ↓ (Lesson.LabId, nullable)
Lab
```

Confirmed decisions, none reversed:
- **No `Module.SyllabusId`.** Module stays Course-scoped only (matches live
  schema — see forensic audit).
- **No reversal to `Lab.LessonId`.** The FK stays on Lesson pointing at Lab.
- **Course is a valid, mandatory intermediate layer** between Syllabus and
  Module — there is no direct Syllabus→Module link, and none is needed (the
  live data confirms Course already carries `DisplayOrder`/`EstimatedHours`/
  `SubjectArea` in its own right, i.e. Course is a real content layer, not a
  pass-through).

## STEP 20 — Standard syllabus semantics

**`IsSystemOwned` is confirmed, empirically, to mean exactly "Standard/System
Syllabus".** The one real live Syllabus row has `IsSystemOwned = true` and
represents precisely what the business ERD calls a "Standard Syllabus" (a
platform-wide curriculum, not tied to any one school — its linked Course has
`SchoolId = NULL`). This is not inferred from the column name alone; it's
observed real usage.

Decision: **use `IsSystemOwned` as-is.** Do **not** add `IsStandard`,
`SchoolId`, or `SourceSyllabusId` to `Syllabus` — none of these are needed and
none would be additive-safe reinventions of something that already works.

No ambiguity found. Not stopping.

## STEP 23 — GradeLevel audit

Live `GradeLevels` currently has exactly one row (`Level=12`, `Code="G_12"`,
`Name="Khối 12"`). The table structure supports an arbitrary number of grade
levels (Level is a plain `int`, `Code`/`Name` are free text) — nothing in the
schema hardcodes 10/11/12. **No business logic should hardcode grade numbers
either** — `GradeLevel` is reference data, sourced from the `GradeLevels`
table via its own list endpoint, not a hardcoded enum. Levels 10/11 simply
don't exist as rows yet; that's a data-seeding gap, not a schema or code gap,
and is out of scope for RED-2 (no seed data invented here).

## STEP 30 — School adoption audit

The live `Course` row (`Id=40`) already demonstrates the exact mechanism STEP
30 asks about:

```
Standard Syllabus (Id=1, IsSystemOwned=true)
        ↓ SyllabusId
Course (Id=40, SchoolId=NULL)   ← the system's own reference course
```

A school "adopting" this standard syllabus is representable, with the exact
same columns, as **a second, separate `Course` row**: same `SyllabusId=1`,
but with `SchoolId` set to that school's id instead of `NULL`. No new table is
needed — `Course.SchoolId` + `Course.SyllabusId` together already fully
express "which school is using which standard syllabus, via which course
instance."

**Decision: do NOT create a `SchoolSyllabus` table.** Flow, confirmed:

```
System Syllabus (SchoolId not applicable — lives on Course, not Syllabus)
      ↓
Course
 ├─ SchoolId  (NULL = system/reference course, set = a school's adopted instance)
 └─ SyllabusId (always points back to the standard syllabus)
```

**Gap, not resolved here:** if a school later needs to *independently edit*
its adopted curriculum (different modules/lessons than the standard), the
current model would require a full second Module/Lesson tree under that
school's Course — which already works structurally today, but has no
"diff from standard" or "re-sync from standard" mechanism. Flagged as a
future gap, not addressed in RED-2 per instruction (audit only, no redesign).
