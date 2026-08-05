using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

// Runtime adapter cho L298N Motor Driver — suy ra trạng thái động cơ (forward/
// backward/stopped/brake) từ cặp chân IN tương ứng, theo đúng bảng sự thật
// L298N chuẩn. KHÔNG đọc ENA/ENB (QEMU chỉ instrument digitalWrite qua macro
// SF_EVENT, không có analogWrite/ledcWrite PWM duty-cycle) — coi như luôn
// enabled, đúng giới hạn kỹ thuật đã ghi trong VIRTUAL_LAB_PLAN.md.
public sealed class L298nModel
{
    public L298nModel(string partId, string? in1Pin, string? in2Pin, string? in3Pin, string? in4Pin)
    {
        PartId = partId;
        In1Pin = in1Pin;
        In2Pin = in2Pin;
        In3Pin = in3Pin;
        In4Pin = in4Pin;
    }

    public string PartId { get; }
    public string? In1Pin { get; }
    public string? In2Pin { get; }
    public string? In3Pin { get; }
    public string? In4Pin { get; }

    // Trạng thái pin hiện tại — mutable, riêng cho 1 lần chạy (instance mới mỗi
    // ExecuteInBackgroundAsync, không static/shared giữa các run khác nhau).
    public bool? In1Value { get; set; }
    public bool? In2Value { get; set; }
    public bool? In3Value { get; set; }
    public bool? In4Value { get; set; }

    public static string ComputeState(bool? a, bool? b)
    {
        // Chưa nhận đủ digitalWrite cho cả 2 chân của cặp này -> coi như
        // "stopped" (an toàn, không suy đoán trạng thái khi thiếu dữ liệu).
        if (a is null || b is null) return "stopped";
        if (a == true && b == false) return "forward";
        if (a == false && b == true) return "backward";
        if (a == false && b == false) return "stopped";
        return "brake"; // a == true && b == true
    }

    public SimulationEventResponse ToMotorStateEvent(long time, string motorLabel, string state)
    {
        return new SimulationEventResponse
        {
            Type = "part-state",
            Time = time,
            Payload = new Dictionary<string, object?>
            {
                ["partId"] = PartId,
                ["component"] = "l298n",
                ["motor"] = motorLabel,
                ["state"] = state
            }
        };
    }
}
