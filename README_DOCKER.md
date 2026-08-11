# Docker image cho StemFlow Virtual Lab — hướng dẫn dùng chung cho team

## Vì sao cần Docker

Tính năng **Virtual Lab** (compile code ESP32 + chạy mô phỏng qua QEMU) chạy
sandbox hoá hoàn toàn trong Docker — không cài trực tiếp arduino-cli/QEMU lên
máy host. Có 2 image liên quan:

| Image | Dùng để | Dockerfile |
|---|---|---|
| `stem-arduino-cli-sandbox` | Compile sketch Arduino/ESP32 (arduino-cli, core `esp32:esp32@2.0.17`) | `docker/simulation-compile-sandbox/Dockerfile` |
| `stem-qemu-runner-sandbox` | Chạy firmware ESP32 đã compile trong QEMU | `docker/simulation-qemu-sandbox/Dockerfile` |

Tài liệu này tập trung vào `stem-arduino-cli-sandbox` — image build **chậm**
(phải tải toolchain ESP32 ~300MB+ lần đầu) nên team dùng chung 1 bản đã build
sẵn trên Docker Hub thay vì mỗi người tự build.

> **Docker vẫn phải cài trên máy local** nếu bạn muốn test full simulation
> (Run button trong Virtual Lab) — pull image có sẵn chỉ giúp bạn không phải
> tự **build** nó, không thay thế được việc cần **có** Docker Desktop chạy.

## ⚠️ Core version — không được đổi ngược

Image này **bắt buộc** pin `esp32:esp32@2.0.17` (không phải bản mới nhất).
Core `3.3.10`/IDF5 trở lên đã xác nhận **crash không tất định** trong
`qemu-system-xtensa` ("Guru Meditation Error: Cache error", có thể xảy ra
trước cả khi `setup()` chạy) — xem chi tiết điều tra + bằng chứng ở
`STEM_FE/VIRTUAL_LAB_PLAN.md` mục 8.12/8.13. Nếu bạn tự build lại image, xác
nhận `docker/simulation-compile-sandbox/Dockerfile` vẫn còn dòng:

```dockerfile
RUN arduino-cli core install esp32:esp32@2.0.17
```

## Cho team member — pull image có sẵn (không cần tự build)

### Pull cả 2 image cùng lúc

Để chạy BE local + chức năng compile/chạy mô phỏng ESP32/QEMU, cần cả 2 image:

```powershell
# Pull từ Docker Hub
docker pull trieu77/stem-arduino-cli-sandbox:sensor-v2
docker pull trieu77/stem-qemu-runner-sandbox:qemu-v1

# Tag lại thành tên mặc định (code BE tìm image theo tên này)
docker tag trieu77/stem-arduino-cli-sandbox:sensor-v2 stem-arduino-cli-sandbox:latest
docker tag trieu77/stem-qemu-runner-sandbox:qemu-v1 stem-qemu-runner-sandbox:latest

# Verify đã pull đúng
docker images | findstr stem
```

Sau khi hoàn tất, chạy BE bình thường — không cần cấu hình thêm.

## Cho chủ Docker Hub repo (`trieu77`) — build/tag/push bản mới

Chỉ người có quyền push lên Docker Hub mới cần chạy bước này (khi core
version đổi, hoặc Dockerfile có thay đổi).

**Arduino CLI sandbox:**

```powershell
docker login
cd docker/simulation-compile-sandbox
./build-and-push.ps1 -Tag sensor-v2
```

**QEMU runner sandbox:**

```powershell
docker login
cd docker/simulation-qemu-sandbox
docker build -t trieu77/stem-qemu-runner-sandbox:qemu-v1 .
docker push trieu77/stem-qemu-runner-sandbox:qemu-v1
```

## Cấu hình backend dùng image từ Docker Hub

`appsettings.json` (KHÔNG commit lên Git — xem `.gitignore`) đọc image name
qua key `SimulationCompile:DockerImage`. Có 2 cách dùng:

**Cách 1 (khuyến nghị, đơn giản nhất):** giữ nguyên giá trị mặc định
`stem-arduino-cli-sandbox:latest` và `stem-qemu-runner-sandbox:latest`,
để lệnh tag ở trên tự đặt đúng tên này — không cần sửa `appsettings.json` gì cả.

**Cách 2:** trỏ thẳng tới tag đầy đủ trên Docker Hub (không cần tag lại local):

```json
"SimulationCompile": {
  "DockerImage": "trieu77/stem-arduino-cli-sandbox:v1-esp32-2.0.17"
}
```

Xem file mẫu đầy đủ ở `STEM.Api/appsettings.Example.json` — copy thành
`appsettings.json` rồi điền secret thật của bạn (connection string, JWT
secret...), phần `SimulationCompile:DockerImage` đã sẵn giá trị Docker Hub.

## Kiến trúc không đổi

Việc chuẩn hoá này **chỉ** thay đổi nguồn lấy image (build local → pull từ
Docker Hub) — không đổi:
- Cách `QemuEsp32Runner`/`SimulationCompileService` gọi Docker (`--network none`,
  `--cap-drop ALL`, `--read-only`, non-root user, resource limits... giữ nguyên).
- Sandbox vẫn bắt buộc, không có đường tắt bỏ qua Docker.
- Core version vẫn pin `2.0.17` (arduino-cli sandbox).
- Mỗi image có vai trò riêng: compile sandbox (arduino-cli + toolchain) và
  runner sandbox (QEMU thực thi firmware đã compile).

## Lưu ý bảo mật

- **Không** commit `appsettings.json` thật (đã có trong `.gitignore`) — chỉ
  commit `appsettings.Example.json` với giá trị placeholder.
- **Không** commit Docker Hub password/token — `docker login` chạy tương tác
  trên máy bạn, không lưu credential vào script hay file nào trong repo.
- Nếu Docker Hub repo cần private, đảm bảo mọi thành viên team đã được thêm
  quyền pull trước khi dùng lệnh pull (không script tự xử lý đăng nhập).
