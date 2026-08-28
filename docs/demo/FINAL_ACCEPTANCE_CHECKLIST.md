# STEM Virtual Lab — Final Acceptance Checklist

Manual, click-by-click checklist for a human to run in a real authenticated
browser session. Generated because the AI assistant performing this audit
has no authenticated browser session (no login credentials, and creating
one is out of scope for an AI agent) — every item below was verified at the
API/runtime/source level (real Docker/QEMU compiles, real unit tests) but
**not** visually confirmed in the rendered UI. Status: **BROWSER_USER_VERIFICATION_REQUIRED**.

Run through all 12 tests in order on the actual deployed/dev app. Check the
box that matches what you observe — don't check PASS from memory, look at
the screen.

---

## TEST 1 — LED

**ACTION**: Open a Lab/Sandbox with an LED wired to an ESP32 GPIO
(e.g. Robot Delivery LAB01, or any "Chọn bài tập mẫu" exercise with a plain
LED). Click Run. Wait for compile to finish.

**EXPECTED**: LED visual turns on/off in sync with `digitalWrite(LED_PIN,
HIGH/LOW)` in the sketch — matches the Serial Monitor's printed state, no
flicker/desync, no fake blinking that ignores the actual code.

PASS ☐ FAIL ☐

---

## TEST 2 — Button → LED

**ACTION**: Open the Push Button + LED exercise (or any lab wiring a button
input to an LED output). Click Run. Press and hold the on-canvas button
control, then release.

**EXPECTED**: LED responds to the button press in real time (not a canned
animation) — LED state changes only while/after the button is actually
pressed per the sketch's logic, and reverts correctly on release.

PASS ☐ FAIL ☐

---

## TEST 3 — Analog Sensor (Potentiometer / Light Sensor)

**ACTION**: Open a Potentiometer or Photoresistor exercise. Click Run. Drag
the on-canvas slider through its full range.

**EXPECTED**: The value read by `analogRead()` in the Serial Monitor tracks
the slider position in real time (0-4095 ESP32 ADC range), and any
threshold-driven output (LED, buzzer) reacts exactly at the coded threshold
— not before, not after.

PASS ☐ FAIL ☐

---

## TEST 4 — DHT22 (Temperature + Humidity)

**ACTION**: Open "Trạm đo nhiệt độ độ ẩm DHT" (Bài 10). Click Run. Watch
Serial Monitor for at least 15 seconds (the shipped scenario has 4
timeline points: 0s/25°C, 5s/30°C, 9s/38°C, 14s/26°C).

**EXPECTED**: Compile succeeds (no "StemFlowDHT.h: No such file" error —
this was a real bug fixed earlier in this project). Serial prints
temperature/humidity matching each timeline point in sequence, in the SAME
run (no restart). LED turns ON only during the 38°C sample (> 35°C
threshold) with "CANH BAO: NHIET DO CAO!" printed, then OFF again.

PASS ☐ FAIL ☐

---

## TEST 5 — HC-SR04 (Distance)

**ACTION**: Open an HC-SR04 distance exercise (or Robot Delivery LAB04/05).
Click Run. Watch Serial Monitor as the configured sensorScenario timeline
plays out.

**EXPECTED**: `Khoang cach: NN.NN cm` printed values match the configured
timeline exactly at each timestamp, and any LED/buzzer warning tied to a
distance threshold fires at the correct moment — no restart needed for the
value to change mid-run.

PASS ☐ FAIL ☐

---

## TEST 6 — L298N (Motor Driver)

**ACTION**: Open Robot Delivery LAB02 or LAB03 (single/dual motor). Click
Run.

**EXPECTED**: The motor card(s) show the correct Tiến/Lùi/Dừng (forward/
backward/stop) label matching the sketch's `digitalWrite(IN1/IN2/...)`
calls, AND — since Motor Animation was added — the DC Motor's shaft/hub
visibly spins while forward/backward, and freezes while stopped/braked.
Only the shaft/hub should spin; the motor body/case and lead wires must
stay visually still.

PASS ☐ FAIL ☐

---

## TEST 7 — LAB06 (Robot Delivery — Stop on Obstacle)

**ACTION**: Open "[Robot Giao Hàng Mini] LAB06". Click Run. Watch for at
least 6 seconds (scenario: 0s→100cm, 5s→15cm).

**EXPECTED**: 0-5s: both motor cards show Tiến/Tiến (forward/forward) AND
both motor/wheel visuals spin. After 5s: both flip to Dừng/Dừng
(stopped/stopped) AND both visuals freeze — same run, no restart/recompile
between the two states.

PASS ☐ FAIL ☐

---

## TEST 8 — LAB08 (Robot Delivery — Complete Mini Delivery Robot)

**ACTION**: Open "[Robot Giao Hàng Mini] LAB08". Click Run. Watch the full
~8-second run to completion.

**EXPECTED**, in order: "BAT DAU GIAO HANG" → MOVING (both wheels visibly
spin) → at ~4s, distance drops to 12cm → OBSTACLE then TURNING (**only
ONE** wheel spins during TURNING — check which side; if BOTH wheels spin
during TURNING, this is a FAIL and must be reported) → resumes MOVING (both
wheels spin again) → "DELIVERED" printed exactly once, both wheels frozen
afterward with no further state changes.

PASS ☐ FAIL ☐

---

## TEST 9 — Stop Simulation

**ACTION**: While any lab with a spinning motor/fan is running (e.g. LAB06
mid-run), click "Dừng mô phỏng" (Stop). Wait and watch for at least 10
seconds after clicking Stop.

**EXPECTED**: All motor/fan/propeller animations freeze IMMEDIATELY on
Stop and stay frozen for the full 10+ second observation window — no
further Serial output appears, no motor visual resumes spinning on its own.
(Backend-level: this was already verified with a real 21-second frozen-event
window in a prior automated pass — this test confirms the same holds true
visually in the browser.)

PASS ☐ FAIL ☐

---

## TEST 10 — Reset

**ACTION**: After Stop (or after a completed run), click Reset (or reload
the lab / reopen the sandbox).

**EXPECTED**: Every motor/fan/drone-propeller/wheel visual returns to its
static default pose — nothing keeps spinning, no leftover Tiến/Lùi label,
Serial Monitor clears.

PASS ☐ FAIL ☐

---

## TEST 11 — Save / Reload

**ACTION**: Open any lab with a configured `sensorScenario` (e.g. Bài 10
DHT, or LAB06). Wait for the autosave indicator to show "saved" (or refresh
the page after a few seconds of inactivity). Reload the page / reopen the
same lab.

**EXPECTED**: The diagram, wiring, code, AND the sensor scenario timeline
(temperature/humidity/distance timeline points) are all still present after
reload — nothing reverts to empty/default. If this lab also has a
`mechanicalLinks` declaration (Robot Wheel ↔ Motor explicit link), confirm
that survives reload too.

PASS ☐ FAIL ☐

---

## TEST 12 — Student Submission

**ACTION**: As a student, open a lab linked to an assignment, make sure a
`sensorScenario` is configured (or use LAB06+), then click Submit.
Afterward, view the submission (as the student, or as the teacher grading
it) and inspect the saved diagram/code.

**EXPECTED**: The submitted snapshot reflects the diagram, code, AND sensor
scenario the student actually had at submit time — not a blank/default
scenario. (Known gap at audit time: the submission endpoint historically
dropped `sensorScenario`/`mechanicalLinks` even though the Run/save paths
kept them — confirm whether this was fixed; if the submitted diagram is
missing the scenario timeline compared to what was configured before
submitting, this is a FAIL.)

PASS ☐ FAIL ☐

---

## Summary

| # | Test | Result |
|---|------|--------|
| 1 | LED | ☐ PASS ☐ FAIL |
| 2 | Button → LED | ☐ PASS ☐ FAIL |
| 3 | Analog sensor | ☐ PASS ☐ FAIL |
| 4 | DHT22 | ☐ PASS ☐ FAIL |
| 5 | HC-SR04 | ☐ PASS ☐ FAIL |
| 6 | L298N | ☐ PASS ☐ FAIL |
| 7 | LAB06 | ☐ PASS ☐ FAIL |
| 8 | LAB08 | ☐ PASS ☐ FAIL |
| 9 | Stop | ☐ PASS ☐ FAIL |
| 10 | Reset | ☐ PASS ☐ FAIL |
| 11 | Save/reload | ☐ PASS ☐ FAIL |
| 12 | Student submission | ☐ PASS ☐ FAIL |

Any FAIL here should be reported back with: which test, what you saw
instead of the expected result, and (if possible) a screenshot or the
browser console/network tab error.
