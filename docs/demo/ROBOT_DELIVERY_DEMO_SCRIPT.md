# Robot Delivery Mini — Demo Script

ACCELERATION PHASE 6, STEP 24/25. Target duration: 5-7 minutes.

## Script

1. **Show the module** (30s) — Open Virtual Lab → "Chọn bài tập mẫu". Point
   out the "Robot Giao Hàng Mini — 8 bài — tiến trình" section header
   (Phase 6 grouping fix) instead of 8 loose cards mixed into the other 14
   exercises.
2. **Show the progression** (30s) — Scroll LAB01 → LAB08 titles. One
   sentence per stage: output → one motor → two motors → distance sensor →
   warning → integration gate → avoidance sequence → complete robot.
3. **Open LAB06** (30s) — "Robot dừng khi gặp vật cản". This is the
   integration gate: first lab where sensor and motors run together.
4. **Explain components** (45s) — ESP32 (brain), HC-SR04 (eyes — distance),
   L298N (muscle driver — cầu H), 2 DC Motor (wheels). Point out the DC
   Motor is a passive load — it never reads GPIO directly, L298N does.
5. **Show wiring** (30s) — IN1-4 → L298N, OUT1-4 → motors, TRIG/ECHO →
   HC-SR04. Call out the rule the validator enforces live: motor terminals
   are physically incapable of connecting straight to a GPIO pin in this
   canvas.
6. **Show code** (30s) — `readDistanceCm()` (pulseIn + `/58.0`), then the
   one-line decision: `distance > 30 ? forward() : stopCar()`.
7. **Run** (10s + real compile wait) — Click Run. If the sandbox was opened
   a few seconds earlier, the firmware is likely already precompiled in the
   background (see Fallback below) and this returns almost immediately.
8. **Distance = safe (100cm)** — Both motor cards read **T:Tiến / P:Tiến**
   (Trái/Phải — Forward/Forward).
9. **Distance = obstacle (15cm)** — Scenario timeline crosses the 30cm
   threshold at t=5s. Both motor cards flip to **T:Dừng / P:Dừng**
   (Stop/Stop) — same run, no restart, no recompile.
10. **Explain it scales** — Same L298N + HC-SR04 mechanism, same GPIO pins,
    carries forward unchanged into LAB07 (adds a turn) and LAB08 (adds the
    full mechanical BOM — chassis/wheels/caster/delivery box, all
    visual-only, zero electrical impact).

## Talking points (STEP 25)

- **Why multi-provider exists**: components can be acquired from different
  sources (native catalog, Fritzing, KiCad) — but acquisition is a
  cataloging concern, never a runtime one.
- **Why visual/pin validation matters**: a wrong pin name or a forbidden
  direct motor→GPIO connection is exactly the kind of mistake real
  hardware would let you make destructively (burning a GPIO on 500mA of
  motor current) — the simulator catches it before Run, with a plain-
  English message, not a stack trace.
- **Why QEMU is used**: HC-SR04's `pulseIn()` and L298N's `digitalWrite`
  timing are real ESP32 Arduino-core behavior — QEMU runs the actual
  compiled firmware against a real Xtensa CPU emulation, so what compiles
  and runs here is the same code a real board would accept.
- **Why simulation runtime is isolated from external providers**: once a
  component type is known to the runtime (native catalog or already
  imported), Run never talks to Fritzing/KiCad again — the whole 8-lab
  module uses only native `wokwi-*` types, so it is provider-independent by
  construction, not by a special-cased exception.
- **Why mechanical robot physics is outside current scope**: the learning
  objective is "sensor input → decision → actuator output", which a
  state-based motor model teaches completely — 2D position/collision
  physics would add engineering cost without adding to that objective.
- **Why state-based motor simulation is enough**: forward/backward/
  stopped/brake is derived from the exact IN1-4 truth table a real L298N
  datasheet defines — it's not a simplification of the logic, only of the
  mechanical consequence (wheels don't visibly roll).

## Failure fallback (STEP 26)

The architecture already has a firmware cache warm-up path suited to this:
`LabSandboxPage` triggers `precompile` automatically 2.5s after the student
stops typing (`virtualLabProjectsApi.precompile`, debounced), which
compiles and caches the firmware in the background before Run is ever
clicked. **Practical mitigation, not a fabricated result**: open the LAB06
sandbox 1-2 minutes before presenting and leave it idle — by the time you
click Run live, the firmware is very likely already cached, so the ~20-90s
real compile latency observed in this pass's live verification happens
before the audience is watching, not during. If the Docker/QEMU container
is genuinely unavailable at demo time, there is no cached fallback to a
fake result — the correct move is to say so and fall back to walking
through the diagram/code/expected-Serial-output narratively instead of
clicking Run.

## Provider-independence note (STEP 27)

Not independently re-verified via a live provider-disabled browser session
this pass (browser access is blocked — see main report). Architecturally
guaranteed rather than assumed: all 8 Robot Delivery components resolve
through the native static catalog (`wokwi-l298n`, `wokwi-hc-sr04`,
`wokwi-dc-motor`, etc.), never through `SimulationTypeResolver`'s
Registry-import path (that resolver only maps a handful of generic
imported categories — LED/BUTTON/BUZZER/SERVO — and has no entry for any
Robot Delivery type). Fritzing/KiCad are therefore never consulted for
these labs regardless of whether those providers are enabled.
