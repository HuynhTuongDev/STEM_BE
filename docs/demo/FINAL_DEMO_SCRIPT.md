# STEM Virtual Lab — Final Demo Script

Target duration: 7-10 minutes. Generated as part of the Final Project
Readiness audit (2026-08-25). Does not demo the full ~22-lab catalog —
proves the system works end-to-end, not the catalog size.

Status note: this script was written from real source/API/test verification
(see the accompanying `STEM_VIRTUAL_LAB_FINAL_READINESS` report for exact
evidence per claim). The actual click-by-click walkthrough has not been
performed in a live authenticated browser by this audit — see
`FINAL_ACCEPTANCE_CHECKLIST.md` for the human verification pass that should
happen before presenting this live.

## Script

1. **Syllabus / Lab structure** (45s) — Open the Master Admin "Chương trình
   khung" (Syllabus) page. Show `GradeLevel → Syllabus → Course → Module →
   Lesson → Lab` as a real, DB-backed hierarchy (not a mockup) — a
   `Syllabus` can carry a `GradeLevelId`, and a `Class` separately carries
   its own `GradeLevelId`. Say plainly: this is the platform's own
   authored curriculum framework, not a claim of an official Ministry of
   Education program — nothing in this system asserts that.

2. **Teacher picks a template** (30s) — Open Virtual Lab → "Chọn bài tập
   mẫu". Point at the "Robot Giao Hàng Mini — 8 bài — tiến trình" section
   header, and separately at a standalone lab like Bài 8 ("Cảnh báo rò rỉ
   nước") to show both single-lesson and progression-style content exist.

3. **Button/LED realtime** (45s) — Open a pushbutton+LED exercise, click
   Run, press-and-hold the on-canvas button. LED responds live — this is
   real `ISimulationInputChannel` input, not a canned animation (pushbutton
   is Class A: full runtime + interactive, per `component-compatibility.json`).

4. **Sensor Scenario** (60s) — Open Bài 10 (DHT22). Click the Sensor
   Scenario panel — show the Vietnamese UI (Thời gian/giây, Nhiệt độ/°C, Độ
   ẩm/%), add a timeline point, and point out the inline validation: time
   can't go negative, an out-of-order time mark is flagged with a visible
   warning, and analog/temperature/humidity fields are clamped to sensible
   ranges. Click Run — show the compile succeed (this exact lab was the
   subject of a real, documented compile-pipeline bug fix — the
   `StemFlowDHT.h` missing-header issue — now live-verified end-to-end via
   real Docker/QEMU) and Serial Monitor tracking the configured
   temperature/humidity timeline, LED reacting exactly at the 35°C
   threshold.

5. **LAB06** (60s) — Open "[Robot Giao Hàng Mini] LAB06 — Dừng Xe Khi Gặp
   Vật Cản". Run it. At 100cm both motor cards read Tiến/Tiến and both
   motor/wheel visuals spin (Motor Animation feature); after the scenario
   crosses to 15cm, both flip to Dừng/Dừng and both visuals freeze — same
   run, no restart. See `ROBOT_DELIVERY_DEMO_SCRIPT.md` for the full
   component-by-component wiring narration if more time is available.

6. **LAB08** (60s) — Open "[Robot Giao Hàng Mini] LAB08". Run it and narrate
   the full sequence live: BAT DAU GIAO HANG → MOVING (both wheels spin) →
   OBSTACLE/TURNING (**only one** wheel spins — call this out explicitly,
   it's the single most detail-sensitive part of the whole demo) → resumes
   MOVING → DELIVERED, motors frozen after.

7. **Stop / Reset** (30s) — Mid-run on any lab, click "Dừng mô phỏng".
   Motors/fans freeze immediately and stay frozen (this was verified with a
   real 21-second frozen-event backend test, not just a UI flag). Click
   Reset — everything returns to its static default pose.

8. **Student submission** (45s) — As a student, submit a lab that has a
   configured Sensor Scenario (e.g. Bài 10 or LAB06). Open the submission
   as a teacher/grader and show the saved diagram still carries the sensor
   scenario timeline the student configured — this was a real, confirmed
   gap (`submitVirtualLab` used to silently drop `sensorScenario`/
   `mechanicalLinks`) fixed as part of this audit.

9. **Teacher monitoring / reporting** (30s, if ready) — Open a Lab's stats
   view (`GET /api/labs/{id}/stats`, already implemented and wired to real
   `LabProgress`/`Submission` data — not fabricated for this demo): student
   count, started count, completed count, completion rate, and (if the lab
   is linked to a graded Assignment) the auto-grade pass/fail breakdown per
   submission.

## What this demo deliberately does NOT show

- No 2D robot movement, no drone flight, no physics engine — motors/fans/
  propellers only ever spin/stop in place, matching the product decision.
- No claim that the Syllabus content is an official government curriculum.

## Known Limitations (protocol/runtime gaps — not fixed this task, not faked)

CLOSE REMAINING FINAL-LAB GAPS task (2026-08-25) re-confirmed these 4 gaps
still hold exactly as originally found — restated here verbatim as the
durable record (previously only stated in chat, not persisted in a doc):

- **`PWM_QEMU_GAP`** — No PWM-driven brightness/speed/servo-angle demo under
  QEMU (the production runner). QEMU only instruments `digitalWrite`;
  `analogWrite`/`ledcWrite` are confirmed, definitively, never instrumented
  anywhere in source (`FirmwareCacheService`'s GPIO instrumentation preamble
  only macro-overrides `digitalWrite`).
- **`I2C_CAPABILITY_GAP`** — No I2C bus master/slave emulation exists
  anywhere in this codebase. OLED SSD1306 and IMU MPU6050 both have real
  visuals (`@wokwi/elements`) and real pin geometry (wiring-validation-only),
  but zero I2C runtime — see `component-compatibility.json`'s
  `wokwi-ssd1306`/`wokwi-mpu6050` entries (`I2C_RUNTIME_NOT_SUPPORTED` in
  `missingRequirements`). Neither component is included in the Final Demo
  Lab runtime set.
- **`SPI_NOT_SUPPORTED`** — No SPI bus emulation exists anywhere in this
  codebase (no SPI-based component has ever been wired into any runner).
- **`UART_SECONDARY_NOT_SUPPORTED`** — Only the ESP32's primary UART
  (Serial/UART0, used for Serial Monitor output) is instrumented. No
  secondary/software UART peripheral communication is emulated.

## Failure fallback

Same firmware-cache warm-up mitigation as `ROBOT_DELIVERY_DEMO_SCRIPT.md`:
open the sandbox 1-2 minutes before presenting so the background precompile
finishes before Run is clicked live. If Docker/QEMU is genuinely unavailable
at demo time, fall back to walking through diagram/code/expected Serial
output narratively — never fabricate a result.
