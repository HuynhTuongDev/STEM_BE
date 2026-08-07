using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

// Runtime adapter cho RGB LED — 3 kênh R/G/B ĐỘC LẬP, mỗi kênh chỉ bật/tắt
// (digitalWrite HIGH/LOW), KHÔNG có độ sáng trung gian (QEMU chỉ instrument
// digitalWrite, không có analogWrite/ledcWrite PWM — cùng giới hạn đã ghi
// nhận ở L298nModel/ENA/ENB). Mỗi kênh là 1 GPIO riêng, không ghép cặp như
// L298N — đơn giản hơn, gần với LedModel/BuzzerModel nhất.
public sealed class RgbLedModel
{
    public RgbLedModel(string partId, string? redPin, string? greenPin, string? bluePin)
    {
        PartId = partId;
        RedPin = redPin;
        GreenPin = greenPin;
        BluePin = bluePin;
    }

    public string PartId { get; }
    public string? RedPin { get; }
    public string? GreenPin { get; }
    public string? BluePin { get; }

    public SimulationEventResponse ToChannelEvent(long time, string channel, string value)
    {
        return new SimulationEventResponse
        {
            Type = "part-state",
            Time = time,
            Payload = new Dictionary<string, object?>
            {
                ["partId"] = PartId,
                ["component"] = "rgb-led",
                ["channel"] = channel,
                ["state"] = value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ? "on" : "off"
            }
        };
    }
}
