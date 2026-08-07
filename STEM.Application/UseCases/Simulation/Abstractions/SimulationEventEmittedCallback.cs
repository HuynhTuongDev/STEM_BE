using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Abstractions;

// Gọi ngay sau khi Executor tính ra 1 SimulationEvent — cho phép caller (vd
// EducationalSimulationRunner) ghi/broadcast nó ngay lập tức thay vì phải
// đợi toàn bộ chương trình chạy xong mới có được danh sách event đầy đủ.
public delegate Task SimulationEventEmittedCallback(SimulationEventResponse evt);
