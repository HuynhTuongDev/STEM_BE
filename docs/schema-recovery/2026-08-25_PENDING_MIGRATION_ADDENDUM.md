# Addendum — Pending Migration Found During Final Project Readiness Audit

Generated: 2026-08-25. Read-only audit (`dotnet ef migrations list`, no writes).
This is a small addendum to the existing `DB_MIGRATION_DRIFT.md` (2026-08-22) —
that document remains the authoritative historical forensic record of the
pre-existing `IN_DB_NOT_IN_GIT` drift and is not superseded or rewritten here.
This addendum only records **new** state discovered since that document was
generated.

## New finding: 1 migration not yet applied live

```
dotnet ef migrations list --project STEM.Infrastructure/STEM.Infrastructure.csproj --startup-project STEM.Api/STEM.Api.csproj
...
20260819224626_AddComponentRegistry
20260822120727_AddSystemLogs (Pending)
```

`20260822120727_AddSystemLogs` (introduced in BE commit `a726422`,
"feat(audit): add system audit log persistence") has never been applied to
the live Supabase database. The `SystemLogs` table does not exist there yet.

**Consequence**: `SystemLogService`/`SystemLogRepository` code paths and
`GET /api/system-logs` will throw a Postgres "relation does not exist" error
against the live DB until this migration is applied.

**This was NOT applied by this audit**, per the standing instruction to never
run destructive or state-changing operations against the shared DB
autonomously. Applying it (`dotnet ef database update`) is a manual
deployment action for whoever owns that step — it is additive-only (one new
`CREATE TABLE` + 4 indexes + 1 FK), not destructive, but still requires a
human to run it deliberately against the shared environment.

## Secondary finding: 2 orphaned handwritten migration files (informational, no live risk)

Two files under `STEM.Infrastructure/Migrations/` are invisible to EF's
migration scanner (no paired `.Designer.cs`, no `[Migration(...)]` attribute)
and therefore never show up in `dotnet ef migrations list` at all, not even
as pending:

- `20260726124256_AddRobotDeliveryKitComponentGlueRegistry.cs` — superseded by
  a real, already-run raw SQL script (`SQLScripts/AddRobotDeliveryKitComponentGlueRegistry.sql`).
  Recommend a manual `SELECT` on `ComponentGlueRegistry` to confirm the seed
  rows are present live before treating this file as safe to delete.
- `20260809100000_AddAiQuotaUsage.cs` — creates a singular `"AiQuotaUsage"`
  table; superseded by the later, properly-registered
  `20260812120524_AddAiQuotaUsagesTable.cs` (plural `"AiQuotaUsages"`, confirmed
  applied live). This file is dead code with no live effect — safe to delete
  as repo cleanup, but not urgent.

Both files' own doc-comments explain why they were handwritten: `dotnet ef migrations add`
in this environment mis-resolved to the SQL Server design-time factory instead
of Npgsql, producing bogus type-change migrations — so these two were written
by hand and never went through the normal EF tracking mechanism.

## Deployment gate

Per Phase 11/12 of the Final Project Readiness audit:

```
SYSTEM_LOG_CODE_READY = YES
SYSTEM_LOG_LIVE_READY = NO
```

Before any demo or release that exercises System Log, a human with DB access
must run `dotnet ef database update` (or apply the equivalent SQL) against
the live Supabase instance for `20260822120727_AddSystemLogs`.
