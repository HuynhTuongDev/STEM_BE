# STEM_BE

Clean Architecture ASP.NET Core 9.0 Monolithic Application

## Cấu trúc Dự án

### Shared Libraries
- **Stem.Shared**: Common types, utilities, Result<T> pattern
- **Stem.Observability**: Logging, Monitoring, Tracing (Serilog, Jaeger, OpenTelemetry)
- **Stem.Messaging**: Message Queue, Events (MassTransit + RabbitMQ)
- **Stem.ServiceDefaults**: Default configurations

### Các Lớp Ứng Dụng

#### STEM.Core (Domain Layer)
```
STEM.Core/
├── Entities/
│   ├── Orders/
│   ├── Payments/
│   └── Products/
├── Repositories/
│   ├── IOrderRepository.cs
│   ├── IPaymentRepository.cs
│   └── IProductRepository.cs
├── STEM.Core.csproj
└── (Business entities và repository interfaces)
```

#### STEM.Application (Application Layer)
```
STEM.Application/
├── UseCases/
│   ├── Orders/
│   │   ├── CreateOrderHandler.cs
│   │   ├── GetOrderHandler.cs
│   │   └── UpdateOrderHandler.cs
│   ├── Payments/
│   │   └── ProcessPaymentHandler.cs
│   └── Products/
│       └── GetProductHandler.cs
├── Dtos/
│   ├── Orders/
│   │   ├── CreateOrderDto.cs
│   │   └── OrderResponseDto.cs
│   ├── Payments/
│   │   └── PaymentDto.cs
│   └── Products/
│       └── ProductDto.cs
├── STEM.Application.csproj
└── (Use cases, DTOs, business logic)
```

#### STEM.Infrastructure (Infrastructure Layer)
```
STEM.Infrastructure/
├── Data/
│   ├── StemDbContext.cs
│   └── DbContextFactory.cs
├── Repositories/
│   ├── OrderRepository.cs
│   ├── PaymentRepository.cs
│   └── ProductRepository.cs
├── Extensions/
│   ├── ServiceCollectionExtensions.cs
│   └── DbContextExtensions.cs
├── STEM.Infrastructure.csproj
└── (Database, repository implementations, external services)
```

#### STEM.Api (Presentation Layer)
```
STEM.Api/
├── Controllers/
│   ├── OrdersController.cs
│   ├── PaymentsController.cs
│   └── ProductsController.cs
├── Properties/
│   └── launchSettings.json
├── Program.cs
├── STEM.Api.csproj
└── (API endpoints, Swagger configuration, routing)
```

## Các Tính Năng
- Products Management
- Orders Management
- Payments Processing

## Công Nghệ Sử Dụng
- **Framework**: ASP.NET Core 9.0
- **Database**: Entity Framework Core + SQL Server
- **Messaging**: MassTransit + RabbitMQ
- **Observability**: Serilog, Jaeger, OpenTelemetry

## Hướng Dẫn Chạy
```bash
dotnet restore
dotnet build
dotnet run --project STEM.Api
```

## AI Assistant / Beeknoee Setup

Tính năng AI Assistant trong Virtual Lab gọi Beeknoee Platform (OpenAI-compatible Chat
Completions API). `appsettings.json` đã có sẵn khung cấu hình:

```json
"Beeknoee": {
  "BaseUrl": "https://platform.beeknoee.com/api/v1",
  "Model": "claude-sonnet-4-6",
  "ApiKey": ""
}
```

Có 3 nhóm người dùng khác nhau, mỗi nhóm setup khác nhau:

| Nhóm | Cần làm gì |
|---|---|
| **Local Development** (dev trên máy cá nhân) | Điền key trực tiếp vào `Beeknoee:ApiKey` trong `appsettings.json` — xem bên dưới |
| **Production Deployment** (người deploy server) | Set secret qua `appsettings.Production.json` (server-local) hoặc secret manager của hosting — xem mục "Production Deployment" |
| **End User** (Teacher dùng web app) | **Không cần làm gì cả** — chỉ đăng nhập và dùng, key đã sẵn có ở server |

### LOCAL DEVELOPMENT — Set API key trên máy của bạn

Local development **chỉ dùng duy nhất** `STEM.Api/appsettings.json` — không cần Environment
Variable, không cần User Secrets, không cần restart Visual Studio vì biến môi trường.

1. Pull source mới nhất.
2. Mở `STEM.Api/appsettings.json`.
3. Tìm section:
   ```json
   "Beeknoee": {
     "BaseUrl": "https://platform.beeknoee.com/api/v1",
     "Model": "claude-sonnet-4-6",
     "ApiKey": ""
   }
   ```
4. Điền API key được Team Lead cung cấp vào `ApiKey`:
   ```json
   "ApiKey": "<BEEKNOEE_KEY_TEAM_LEAD_CUNG_CAP>"
   ```
5. Start Backend (không cần restart Visual Studio vì lý do biến môi trường — chỉ cần
   backend build/start lại như bình thường để nạp lại `appsettings.json`).

### ⚠️ CẢNH BÁO BẮT BUỘC — KHÔNG COMMIT API KEY

`appsettings.json` là file **đã được `.gitignore` chặn** trong repo này (xem `.gitignore`,
pattern `appsettings.json` + `appsettings.*.json`) — nghĩa là mặc định Git sẽ không track file
này. Tuy nhiên đây vẫn là file dễ nhầm nhất vì bạn sửa trực tiếp key thật vào nó, nên **luôn
tự kiểm tra trước khi commit/push**, đặc biệt nếu repo thật của bạn có cấu hình `.gitignore`
khác:

```bash
git status
git diff -- STEM_BE/STEM.Api/appsettings.json
git grep -n "sk-bee-"
```

Nếu bất kỳ lệnh nào cho thấy `appsettings.json` đang staged/tracked hoặc chứa `sk-bee-...`
thật: **DỪNG PUSH**, gỡ file khỏi staged/source trước.

Trước khi commit bất kỳ thay đổi nào (kể cả không liên quan tới Beeknoee), nên đưa `ApiKey`
về lại rỗng nếu bạn nghi ngờ file đang bị track:
```json
"ApiKey": ""
```

**Nếu key thật từng bị push lên Git:** coi key đó là **compromised** — phải revoke/rotate
trên Beeknoee ngay, không chỉ xoá ở commit mới (key cũ vẫn còn trong lịch sử git).

Dự án hiện **không** tự động chạy `git update-index --assume-unchanged` hay `--skip-worktree`
cho `appsettings.json` — nếu team muốn dùng cách này để tránh diff local hiện lên git status,
đó là lựa chọn chủ động của team, tự document/thực hiện riêng.

### PRODUCTION DEPLOYMENT

Khi deploy lên server thật, **end user (Teacher) không cần tự cấu hình gì** — key phải được
người deploy chuẩn bị sẵn ở phía server, theo một trong hai cách sau (dùng cách nào tuỳ hạ tầng
hosting thực tế, không cần sửa code cho cả hai):

**Cách 1 — `appsettings.Production.json` trên server:**

ASP.NET Core tự động nạp `appsettings.{ASPNETCORE_ENVIRONMENT}.json` đè lên `appsettings.json`
— đây là hành vi mặc định của framework, **không cần code loader riêng**. Khi server chạy với
`ASPNETCORE_ENVIRONMENT=Production`, chỉ cần tạo file `appsettings.Production.json` **trực tiếp
trên server** (không phải trong repo) từ mẫu `appsettings.Production.example.json` (đã có sẵn
trong repo, không chứa secret):

```json
{
  "Beeknoee": {
    "ApiKey": "<SECRET_THAT_ONLY_LIVES_ON_SERVER>"
  }
}
```

Không cần lặp lại `BaseUrl`/`Model` — ASP.NET Core merge theo key, chỉ `ApiKey` bị ghi đè, phần
còn lại vẫn kế thừa từ `appsettings.json`. File `appsettings.Production.json` **đã được
`.gitignore` chặn** (pattern `appsettings.*.json`) — tạo file này trên server (hoặc trong bước
deploy) chứ **không tạo/commit trong repo**.

**Cách 2 — Environment variable / secret manager của hosting:**

Nếu hosting hỗ trợ (Azure App Service Configuration, systemd `EnvironmentFile`, Docker
`-e`/`--env-file`, Kubernetes Secret, v.v.), set `Beeknoee__ApiKey` trực tiếp ở tầng đó — cùng
convention double-underscore như môi trường dev. Không copy key vào Dockerfile, docker-compose
committed, hay bất kỳ file nào build vào image/commit vào source.

Cả 2 cách đều dẫn tới cùng kết quả: `BeeknoeeLabAiProvider.IsConfigured = true` trên server mà
**không ai cần set gì trên máy cá nhân của Teacher**.

## Giấy Phép
MIT
