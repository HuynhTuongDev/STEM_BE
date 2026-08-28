using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

// SIMPLIFIED_ELECTRICAL_MODEL (audited 2026-08-24, MOTOR ANIMATION VERIFICATION
// milestone) — this deliberately models a brushless drone motor as a single
// digitalWrite(IN, ...) pin, mirroring RelayModel/FanModel exactly. A real
// brushless motor is driven through an ESC's 3-phase output, not a direct
// digital pin.
//
// Audit finding: wokwi-esc ALREADY exists in the registry with a matching
// real-world pinout (SupportedPins["wokwi-esc"] = SIG/GND/BATT+/BATT-/OUT+/
// OUT-, VirtualLabDiagramService.cs) but has NO wiring rule and NO runtime
// model of its own (component-compatibility.json: qemuSupport=false,
// dedicatedWiringRule=false). The architecturally correct chain would mirror
// the already-proven DC-Motor->L298N pattern exactly:
//   ESP32 --SIG--> ESC (EscModel.cs, digitalWrite on/off, same shape as this
//   class) --OUT+/OUT---> Drone Motor (no pins of its own, purely mechanical,
//   like DC Motor today) --> Propeller animation.
// This was judged too large to add safely in this pass (needs: a wiring rule
// + runtime model for wokwi-esc, ComponentIndex wiring for it, AND removing
// Drone Motor's own "IN" pin in favor of tracing its OUT+/- wiring back to an
// ESC — a bigger, riskier change than extending the already-verified Fan/
// Relay/LED pattern). Per instruction: kept the simplified direct-pin model
// rather than fake a bigger one — every emitted event is tagged
// electricalModel="SIMPLIFIED_ELECTRICAL_MODEL" so nothing downstream can
// mistake this for the real ESC-mediated design. Never simulates drone
// flight/aerodynamics (product decision, out of scope regardless of which
// electrical model is used).
public sealed class DroneMotorModel
{
    public const string ElectricalModel = "SIMPLIFIED_ELECTRICAL_MODEL";

    public DroneMotorModel(string partId, string gpioPin)
    {
        PartId = partId;
        GpioPin = gpioPin;
    }

    public string PartId { get; }
    public string GpioPin { get; }

    public SimulationEventResponse ToDigitalEvent(long time, string value)
    {
        return new SimulationEventResponse
        {
            Type = "part-state",
            Time = time,
            Payload = new Dictionary<string, object?>
            {
                ["partId"] = PartId,
                ["component"] = "drone-motor",
                ["state"] = value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ? "on" : "off",
                ["pin"] = GpioPin,
                ["electricalModel"] = ElectricalModel
            }
        };
    }
}
