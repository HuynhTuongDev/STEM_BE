-- =====================================================
-- Fix: Submissions unique index blocked resubmit
-- Applied directly against live Supabase DB on 2026-07-19
-- via a scratch Npgsql script (not EF Core migrations — the
-- `dotnet ef migrations` CLI in this environment resolves to a
-- phantom local SQL Server provider instead of the real Npgsql
-- provider used at runtime; root cause not yet investigated).
-- =====================================================
--
-- Root cause: "IX_Submissions_AssignmentId_StudentId" was a UNIQUE
-- INDEX on (AssignmentId, StudentId) — found live on the DB but not
-- present in any EF Core migration file (schema drift, created
-- outside migrations). This silently capped every student to exactly
-- 1 Submission per Assignment ever, even though Assignment already
-- had AllowResubmit/ResubmitLimit columns and SubmitVirtualLabAsync
-- already computed AttemptNumber (MAX+1) assuming multiple rows per
-- (AssignmentId, StudentId) were possible. A 2nd submit attempt for
-- the same assignment failed with a raw 500 duplicate-key error.
--
-- Fix: widen the unique index to (AssignmentId, StudentId,
-- AttemptNumber) — allows the append-only attempt history the code
-- already assumed, while still preventing two rows from ever sharing
-- the same attempt number for the same student+assignment (the
-- concurrency guard SubmitVirtualLabAsync relies on when two
-- near-simultaneous submits race to compute the same AttemptNumber).
-- Enforcement of AllowResubmit/ResubmitLimit itself lives in
-- application code (SubmitVirtualLabAsync), not in this index.
--
-- Safe to run on live data as-is: since the old index allowed at most
-- 1 row per (AssignmentId, StudentId), no existing row can violate
-- the new 3-column index.
-- =====================================================

DROP INDEX "IX_Submissions_AssignmentId_StudentId";

CREATE UNIQUE INDEX "IX_Submissions_AssignmentId_StudentId_AttemptNumber"
  ON "Submissions" ("AssignmentId", "StudentId", "AttemptNumber");

-- ---------------------------------------------------------------
-- Verify
-- ---------------------------------------------------------------
SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'Submissions' ORDER BY indexname;
SELECT 'Submissions resubmit unique index fix applied.' AS result;
