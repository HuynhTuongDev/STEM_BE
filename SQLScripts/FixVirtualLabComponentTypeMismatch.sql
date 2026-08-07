-- =====================================================
-- Fix: VirtualLab component type mismatch (led vs wokwi-led)
-- Applied directly against live Supabase DB on 2026-07-18/19
-- via a scratch Npgsql script (not EF Core migrations — the
-- `dotnet ef migrations` CLI in this environment resolves to a
-- phantom local SQL Server provider instead of the real Npgsql
-- provider used at runtime; root cause not yet investigated).
-- This file is a historical record + safe to re-run (idempotent
-- UPDATEs keyed by primary key / fixed GUID).
-- =====================================================
--
-- Root cause: ComponentGlueRegistry was seeded (STEM.Infrastructure
-- StemDbContext.cs HasData) with short-form ComponentType values
-- ("led", "buzzer", ...) while VirtualLabDiagramService.Analyze()
-- (BE) only recognizes "wokwi-*"-prefixed types. This silently
-- broke: (1) the teacher add-component palette showing types
-- Analyze() would then reject as unrecognized, and (2) — discovered
-- later — every real Lab.CircuitConfigJson written before this fix
-- also used the short-form type in parts[], which is now rejected
-- by LabService.EnsureAllowedComponentsSupportedAsync on every Save
-- (CreateLabModal.tsx recomputes AllowedComponentTypesJson fresh from
-- parts[].type on every submit, so this can't be patched by fixing
-- AllowedComponentTypesJson alone — parts[].type is the actual
-- source of truth read at Save time).
--
-- StemDbContext.cs's HasData seed source itself is intentionally
-- NOT fixed here (would require an EF Core migration; deferred
-- until the CLI provider-resolution issue above is understood).
-- New/local DB setups will still seed the old short-form values
-- until that follow-up is done.

-- ---------------------------------------------------------------
-- 1. ComponentGlueRegistry: rename ComponentType (PK) to wokwi-* form.
--    Safe as UPDATE (not DELETE+INSERT): confirmed no FK references
--    ComponentGlueRegistry.ComponentType anywhere in the schema, and
--    no runtime code stores a reverse-reference to the old value.
-- ---------------------------------------------------------------
UPDATE "ComponentGlueRegistry" SET "ComponentType" = 'wokwi-led', "UpdatedAt" = now() WHERE "ComponentType" = 'led';
UPDATE "ComponentGlueRegistry" SET "ComponentType" = 'wokwi-buzzer', "UpdatedAt" = now() WHERE "ComponentType" = 'buzzer';
UPDATE "ComponentGlueRegistry" SET "ComponentType" = 'wokwi-servo', "UpdatedAt" = now() WHERE "ComponentType" = 'servo';
UPDATE "ComponentGlueRegistry" SET "ComponentType" = 'wokwi-pushbutton', "UpdatedAt" = now() WHERE "ComponentType" = 'push_button';
UPDATE "ComponentGlueRegistry" SET "ComponentType" = 'wokwi-potentiometer', "UpdatedAt" = now() WHERE "ComponentType" = 'potentiometer';
-- NOTE: dht22 intentionally left as short-form (out of scope per explicit decision;
-- it was already Supported=false / not rendered by CircuitCanvas.tsx before this fix).

-- ---------------------------------------------------------------
-- 2. Lab.AllowedComponentTypesJson: sync the 3 real Labs that had
--    components (led/buzzer/servo/potentiometer/push_button in
--    CircuitConfigJson) to the new wokwi-* type strings.
-- ---------------------------------------------------------------
-- e82937a8-fb12-49d2-ab3a-b780e44556f8: CircuitConfigJson.parts is empty ([]),
-- AllowedComponentTypesJson is already '[]' — no type string to fix, skipped.
UPDATE "Labs" SET "AllowedComponentTypesJson" = '["wokwi-led", "wokwi-potentiometer", "wokwi-pushbutton", "wokwi-buzzer"]', "UpdatedAt" = now()
  WHERE "Id" = '1f33bcee-57cf-4f1b-a66d-cd98e919bbf2';
UPDATE "Labs" SET "AllowedComponentTypesJson" = '["wokwi-buzzer", "wokwi-led"]', "UpdatedAt" = now()
  WHERE "Id" = '4b07cab0-a800-4287-a8fd-d91dc6839780';

-- ---------------------------------------------------------------
-- 3. Lab.CircuitConfigJson parts[].type: the actual source of truth
--    read by CreateLabModal.tsx at Save time (step 2 above patches a
--    field that gets silently recomputed and overwritten on next Save
--    if this step is skipped). Rename-only: x/y/attrs/pinMapping/id/
--    connections/board left untouched.
--
--    Lab[test] and Lab[tes1] keep BoardType = 'arduino_uno' (standing
--    decision: do not force these 2 Labs to ESP32). This means
--    VirtualLabDiagramService.Analyze() will still correctly report
--    "Diagram must include an ESP32 board." for both — expected,
--    not a bug. Save (this fix) and Analyze() validity are treated
--    as two independent, decoupled concerns for these 2 Labs.
-- ---------------------------------------------------------------
UPDATE "Labs" SET "CircuitConfigJson" =
  '{"board": "arduino_uno", "parts": [{"x": 300, "y": 80, "id": "led-1783581924091-0", "type": "wokwi-led", "attrs": {"color": "red"}, "pinMapping": {"pin": 3}}, {"x": 420, "y": 80, "id": "potentiometer-1783581925299-1", "type": "wokwi-potentiometer", "attrs": {}, "pinMapping": {"pin": 2}}, {"x": 540, "y": 80, "id": "push_button-1783581926322-2", "type": "wokwi-pushbutton", "attrs": {}, "pinMapping": {"1": 2}}, {"x": 300, "y": 170, "id": "buzzer-1783581927475-3", "type": "wokwi-buzzer", "attrs": {}, "pinMapping": {"pin": 2}}], "connections": []}'::jsonb,
  "UpdatedAt" = now()
  WHERE "Id" = '1f33bcee-57cf-4f1b-a66d-cd98e919bbf2';

UPDATE "Labs" SET "CircuitConfigJson" =
  '{"board": "esp32_devkit_v1", "parts": [{"x": 80, "y": 320, "id": "buzzer-1783950774510-0", "type": "wokwi-buzzer", "attrs": {}, "pinMapping": {}}, {"x": 210, "y": 320, "id": "led-1783950776704-1", "type": "wokwi-led", "attrs": {"color": "red"}, "pinMapping": {}}], "connections": []}'::jsonb,
  "UpdatedAt" = now()
  WHERE "Id" = '4b07cab0-a800-4287-a8fd-d91dc6839780';
-- NOTE: this Lab's JSON "board" field ("esp32_devkit_v1") still disagrees with the
-- BoardType column ("arduino_uno") — a separate, already-identified mismatch, not
-- touched by this fix (out of scope: only parts[].type was authorized to change).

-- ---------------------------------------------------------------
-- 4. Lab[132213]: same parts[].type rename as above (led -> wokwi-led).
--    A synthetic ESP32 "board" part in parts[] was considered (to make
--    Analyze() recognize the board) but explicitly REJECTED: it forces
--    ComponentGlueRegistry to carry a non-component "board" entry
--    (Supported=true, required to pass Save validation) which then
--    leaks into the teacher add-component palette as an unlabeled
--    "Unknown Component". Rejected in favor of the real fix recorded
--    as a backlog item in VIRTUAL_LAB_PLAN.md: Analyze() should read
--    board type from the diagram's top-level "board" field instead of
--    requiring a literal ESP32 entry inside parts[].
--    BoardType stays 'esp32_devkit_v1' (already correct, matches JSON).
--    Same decoupled Save-vs-Analyze() outcome as Lab[test]/Lab[tes1]:
--    Analyze() still correctly reports "missing ESP32 board" — expected.
-- ---------------------------------------------------------------
UPDATE "Labs" SET "CircuitConfigJson" =
  '{"board": "esp32_devkit_v1", "parts": [{"x": 80, "y": 320, "id": "led-1784012343749-0", "type": "wokwi-led", "attrs": {"color": "red"}, "pinMapping": {}}], "connections": []}'::jsonb,
  "UpdatedAt" = now()
  WHERE "Id" = 'ca5f1ea5-3bdf-4287-8198-244491d19d97';

-- ---------------------------------------------------------------
-- Verify
-- ---------------------------------------------------------------
SELECT "ComponentType", "Label", "Supported" FROM "ComponentGlueRegistry" ORDER BY "ComponentType";
SELECT 'VirtualLab component type mismatch fix applied.' AS result;
