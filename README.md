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

## Giấy Phép
MIT
