# Robot Delivery Mini — End-to-End Flow Audit

ACCELERATION PHASE 6 (End-to-End Demo Hardening), STEP 1/2. Read-only code
audit of the real Teacher → Student → Sandbox → Run path, done BEFORE any
browser testing or code changes, per the milestone's own ordering.

Classification legend: **READY** (works today, no changes needed) ·
**PARTIAL** (works but with a real, named limitation) · **MISSING** (no
code path exists) · **BROKEN** (code path exists but is provably wrong).

## Flow table

| Link | Status | Evidence |
|---|---|---|
| Teacher login | READY | Real JWT auth (`AuthController`/`JwtProvider`) — but see **Blocker** below: no seeded/documented Teacher account exists for a fresh environment. |
| Template picker route | READY | `/dashboard/virtual-lab` → `VirtualLabPage.tsx` → "Chọn bài tập mẫu" opens `TemplatePickerModal`. |
| Robot Delivery module visible | PARTIAL → fixed this pass | Was a flat list intermixed with the 14 other exercises, distinguishable only by title prefix `[Robot Giao Hàng Mini] LABxx`. Fixed: added an optional `module` field to `VirtualLabSampleExercise`, set on all 8 labs, and `TemplatePickerModal.tsx` now renders a grouped section header ("Robot Giao Hàng Mini — 8 bài — tiến trình") instead of mixing them into the flat grid. Low-risk, additive, no API/schema change. |
| Create Lab from template | READY | `TemplatePickerModal` → `CreateLabModal` (pre-filled, forced `status: draft`) → `POST /api/labs` (`LabsController.CreateLab` → `LabService.cs:162-163`). |
| Class assignment | READY (deferred, not blocking) | `classIds` optional at create time (only required when publishing — `LabService.cs` `EnsureCanUseClassesAsync`); same `CreateLabModal` multi-select handles create AND later edit (`SyncClassAssignments`, `LabService.cs:1045-1054`). |
| Student discovery | READY | `GET /api/labs` scoped by class; `StudentClassDetailPage.tsx`'s `VirtualLabsTab` already patched to use this real endpoint (comment there documents an earlier dead legacy endpoint was removed from this path). |
| Sandbox opening | READY | `/dashboard/virtual-lab/:id/sandbox` → `LabSandboxPage.tsx`. `VirtualLabProject` is auto-created transparently on first open (deterministic GUID from `(labId, studentId)`, seeded from the Lab's `circuitConfig`/`starterCode`) — no explicit "create workspace" step for the user. |
| Diagram persistence | READY | Single debounced autosave (1500ms) covers diagram + code together via `PUT /diagrams/{projectId}`; reload restores from the persisted `VirtualLabProject`, confirmed by reading `LabSandboxPage.loadLab`. |
| Code persistence | READY | Same mechanism as diagram (one combined save call). |
| Runner selection (Educational vs QEMU) | **PARTIAL — real limitation, documented, not fixed** | `ISimulationRunnerResolver.Resolve(mode)` is a pure string switch with **zero awareness of diagram contents**. The `mode` string comes from a single global config, `SimulationRunner:DefaultMode` (`appsettings.json`, currently `"qemu"`), applied to every lab/session — there is no per-lab or per-component override anywhere in the codebase. **Consequence:** LAB01 does NOT actually run under "Educational" in production as originally assumed in the Phase 5 report — it runs under whatever `DefaultMode` is set to (`qemu`) globally, same as every other lab. Verified this is not a demo blocker: LAB01's plain digitalWrite/delay code was compiled for real through the live API + Docker sandbox this pass and succeeded (HTTP 200, `success:true`, 0 errors) — so it works correctly under QEMU too. **Not redesigning runner selection** per the milestone's explicit instruction ("Do not redesign runner selection unless needed for demo correctness") — it is not needed; documenting the limitation is sufficient. |
| Results / submission | PARTIAL | `Lab.LinkedAssignmentId` → real submission path exists (`LabSandboxPage.handleSubmit` → `POST /submissions/virtual-lab`) and has a real teacher review UI (`SubmissionDetailPage.tsx`). None of the 8 Robot Delivery templates set `linkedAssignmentId` (defaults to null) — by design, per Phase 5's explicit "keep submission outside current scope" instruction. `LabProgress` (open/complete tracking) works regardless, with no assignment link required. |

## Blocker found this pass: no legitimate way to obtain a Teacher session

Traced the full account-creation path for browser testing:

- `POST /schools/register` (`SchoolsController.cs:36`, `[AllowAnonymous]`) is
  the only self-service account creation entry point. It sets
  `School.Status = Pending` and `User.IsEmailVerified = false`
  (`RegisterSchoolHandler.cs:53,74`) — the response text itself states
  "Please check your email to verify your account. After verification,
  your account will require Master Admin approval."
- Creating a Teacher directly requires an authenticated School Admin
  (`TeachersController.cs:17`, `[Authorize(Policy = "SchoolAdminOnly")]`) —
  which itself only exists after the above two gates (email verification +
  Master Admin approval) are cleared by a human.
- No `DbSeeder`/seed migration creates any `User` row (only
  `SeedRoles.cs` seeds the 4 role names) — confirmed by search.

**Reported per the milestone's own explicit instruction:**
`DEMO_ACCOUNT_CREATION_BLOCKED` — required role: Teacher; required
approval: Master Admin (after email verification); blocking flow:
`POST /schools/register` → email link → Master Admin approval →
School Admin creates Teacher. Minimum legitimate action needed: a human
with an existing Master Admin account (or direct DB access) either
approves a freshly-registered `[DEMO] Robot Delivery E2E` school, or
provides existing Teacher credentials.

## What this means for STEP 6-9 (mandatory browser passes)

LAB01/04/06/08 browser passes are **WAITING_FOR_REAL_BROWSER_LOGIN** — not
skipped, not faked. Everything else in this milestone that does not require
an authenticated browser session (audit, template picker UX, diagram/code
polish, runner documentation, demo script, regression) proceeds normally.
