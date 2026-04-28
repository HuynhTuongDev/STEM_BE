# Kiến Trúc STEM_BE

## Tổng Quan
STEM_BE là một ứng dụng ASP.NET Core 9.0 tuân theo nguyên lý **Clean Architecture** và được thiết kế như một **monolithic application** thay vì microservices.

## Sơ Đồ Lớp Ứng Dụng

```
┌─────────────────────────────────────────┐
│     STEM.Api (Presentation Layer)       │
│  - Controllers (REST API Endpoints)     │
│  - Dependency Injection Setup           │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│   STEM.Application (Application Layer)  │
│  - Use Cases (Business Logic)           │
│  - DTOs (Data Transfer Objects)         │
│  - Application Services                 │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│     STEM.Core (Domain Layer)            │
│  - Entities (Business Objects)          │
│  - Repository Interfaces                │
│  - Domain Logic                         │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│  STEM.Infrastructure (Infrastructure)   │
│  - Entity Framework DbContext           │
│  - Repository Implementations           │
│  - External Service Integrations        │
└─────────────────────────────────────────┘
```

## Shared Libraries

Shared libraries được đặt trong thư mục `Shared/` để cung cấp các tính năng dùng chung:

### Stem.Shared
- Common Result<T> pattern
- Common exceptions
- Extension methods
- Constants

### Stem.Observability
- Serilog logging configuration
- OpenTelemetry tracing setup
- Jaeger integration
- Health checks

### Stem.Messaging
- MassTransit configuration
- RabbitMQ message bus setup
- Event publishers and subscribers
- Message contracts

### Stem.ServiceDefaults
- ASP.NET Core default configurations
- CORS setup
- Authentication/Authorization defaults
- OpenAPI/Swagger configuration

## Các Tính Năng Chính

### 1. Products Management
- **Entity**: Product (ID, Name, Description, Price, Stock)
- **Operations**: Create, Read, Update, Delete, List
- **API Endpoint**: `/api/products`

### 2. Orders Management
- **Entity**: Order (ID, OrderDate, TotalAmount, Status)
- **Relations**: Contains multiple OrderItems
- **API Endpoint**: `/api/orders`

### 3. Payments Processing
- **Entity**: Payment (ID, Amount, Status, PaymentMethod)
- **Relations**: Links to Orders
- **API Endpoint**: `/api/payments`

## Quy Trình Yêu Cầu

```
Client Request
    ↓
API Controller (STEM.Api)
    ↓
Application Service / Use Case (STEM.Application)
    ↓
Domain Entity / Business Logic (STEM.Core)
    ↓
Repository Interface (STEM.Core)
    ↓
Repository Implementation (STEM.Infrastructure)
    ↓
Entity Framework DbContext
    ↓
SQL Server Database
```

## Cấu Hình Cơ Sở Dữ Liệu

- **Provider**: SQL Server
- **ORM**: Entity Framework Core
- **Connection String**: `Server=mssql;Database=Stem_*;User Id=sa;Password=*;`

## Dự Toán Tiếp Theo
- [ ] Add Authentication (JWT)
- [ ] Add Authorization (Role-based)
- [ ] Add Validation (FluentValidation)
- [ ] Add Unit Tests
- [ ] Add Integration Tests
- [ ] Add API Documentation (Swagger)
