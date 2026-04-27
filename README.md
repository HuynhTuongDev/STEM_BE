# STEM_BE

Clean Architecture ASP.NET Core 9.0 Monolithic Application

## Cấu trúc Dự án

### Building Blocks (Shared Libraries)
- **Stem.Shared**: Common types, utilities, Result<T> pattern
- **Stem.Observability**: Logging, Monitoring, Tracing (Serilog, Jaeger, OpenTelemetry)
- **Stem.Messaging**: Message Queue, Events (MassTransit + RabbitMQ)
- **Stem.ServiceDefaults**: Default configurations

### Các Lớp Ứng Dụng
- **STEM.Core**: Domain Layer - Business entities và repository interfaces
- **STEM.Application**: Application Layer - Use cases, DTOs, business logic
- **STEM.Infrastructure**: Infrastructure Layer - Database, repository implementations
- **STEM.Api**: Presentation Layer - API controllers, endpoints

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
dotnet run --project src/STEM.Api
```

## Giấy Phép
MIT
