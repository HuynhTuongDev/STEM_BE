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

## Cấu Trúc Thư Mục Chi Tiết

### Cây Thư Mục Hoàn Chỉnh

```
STEM_BE/
│
├── STEM.Core/                          (Domain Layer - Miền kinh doanh)
│   ├── Entities/
│   │   ├── BaseEntity.cs               (Base class cho tất cả entities)
│   │   ├── Users/
│   │   ├── Orders/
│   │   ├── Payments/
│   │   ├── Products/
│   │   ├── Classes/
│   │   ├── Courses/
│   │   ├── Quizzes/
│   │   ├── Projects/
│   │   ├── Simulations/
│   │   └── (các domain entities khác)
│   ├── Interfaces/                     (Repository Interfaces)
│   │   ├── IRepository.cs              (Generic base interface)
│   │   ├── IUserRepository.cs
│   │   ├── IOrderRepository.cs
│   │   ├── IPaymentRepository.cs
│   │   ├── IProductRepository.cs
│   │   ├── IClassRepository.cs
│   │   ├── ICourseRepository.cs
│   │   ├── IQuizRepository.cs
│   │   ├── IProjectRepository.cs
│   │   ├── ISimulationRepository.cs
│   │   └── (tất cả repository interfaces)
│   ├── DTOs/                           (Data Transfer Objects)
│   │   └── Auth/
│   │       └── (auth-related DTOs)
│   └── STEM.Core.csproj
│
├── STEM.Application/                   (Application Layer - Quy tắc ứng dụng)
│   ├── UseCases/
│   │   ├── Auth/
│   │   │   ├── LoginHandler.cs
│   │   │   ├── RegisterHandler.cs
│   │   │   ├── VerifyEmailHandler.cs
│   │   │   ├── ForgotPasswordHandler.cs
│   │   │   └── ResetPasswordHandler.cs
│   │   ├── Orders/
│   │   │   ├── CreateOrderHandler.cs
│   │   │   ├── GetOrderHandler.cs
│   │   │   ├── UpdateOrderHandler.cs
│   │   │   └── DeleteOrderHandler.cs
│   │   ├── Payments/
│   │   │   ├── ProcessPaymentHandler.cs
│   │   │   └── GetPaymentStatusHandler.cs
│   │   └── Products/
│   │       ├── GetProductHandler.cs
│   │       ├── ListProductsHandler.cs
│   │       └── CreateProductHandler.cs
│   ├── Dtos/
│   │   ├── Auth/
│   │   │   ├── LoginRequest.cs
│   │   │   ├── LoginResponse.cs
│   │   │   ├── RegisterRequest.cs
│   │   │   ├── VerifyEmailRequest.cs
│   │   │   ├── ForgotPasswordRequest.cs
│   │   │   └── ResetPasswordRequest.cs
│   │   ├── Orders/
│   │   │   ├── CreateOrderDto.cs
│   │   │   ├── OrderResponseDto.cs
│   │   │   └── UpdateOrderDto.cs
│   │   ├── Payments/
│   │   │   ├── PaymentRequestDto.cs
│   │   │   └── PaymentResponseDto.cs
│   │   └── Products/
│   │       ├── ProductDto.cs
│   │       ├── CreateProductDto.cs
│   │       └── UpdateProductDto.cs
│   ├── Interfaces/                     (Application Service Interfaces - không phải repository)
│   │   ├── IEmailService.cs            (Email service interface)
│   │   └── IJwtProvider.cs             (JWT token provider interface)
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs (DI setup for handlers)
│   └── STEM.Application.csproj
│
├── STEM.Infrastructure/                (Infrastructure Layer - Triển khai kỹ thuật)
│   ├── Data/
│   │   ├── StemDbContext.cs            (Entity Framework DbContext)
│   │   ├── DbContextFactory.cs
│   │   └── Migrations/                 (EF Core migrations)
│   ├── Repositories/                   (Repository Implementations)
│   │   ├── Repository.cs               (Generic repository base)
│   │   ├── UserRepository.cs
│   │   ├── OrderRepository.cs
│   │   ├── PaymentRepository.cs
│   │   ├── ProductRepository.cs
│   │   ├── (tất cả repository implementations)
│   ├── Services/
│   │   ├── EmailService.cs             (Implements IEmailService - MailKit)
│   │   └── Authentication/
│   │       └── JwtProvider.cs          (Implements IJwtProvider - JWT token generation)
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs (DI registration)
│   │   ├── DbContextExtensions.cs
│   │   └── MappingExtensions.cs
│   └── STEM.Infrastructure.csproj
│
├── STEM.Api/                            (Presentation Layer - API)
│   ├── Controllers/
│   │   ├── OrdersController.cs
│   │   ├── PaymentsController.cs
│   │   ├── ProductsController.cs
│   │   └── (các API endpoints)
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Program.cs                       (Startup configuration, Swagger setup)
│   ├── STEM.Api.csproj
│   └── bin/                             (Build output)
│
├── Shared/                              (Shared Libraries & Building Blocks)
│   ├── Stem.Shared/
│   │   ├── Result<T>/
│   │   │   └── Result.cs
│   │   ├── Exceptions/
│   │   │   ├── ValidationException.cs
│   │   │   └── NotFoundException.cs
│   │   ├── Extensions/
│   │   │   └── StringExtensions.cs
│   │   └── Stem.Shared.csproj
│   ├── Stem.Observability/
│   │   ├── Logging/
│   │   ├── Tracing/
│   │   └── Stem.Observability.csproj
│   ├── Stem.Messaging/
│   │   ├── Events/
│   │   ├── Consumers/
│   │   └── Stem.Messaging.csproj
│   └── Stem.ServiceDefaults/
│       └── Stem.ServiceDefaults.csproj
│
├── STEM.sln                             (Solution file)
├── README.md                            (Tài liệu chính)
├── ARCHITECTURE.md                      (Tài liệu kiến trúc này)
└── .git/                                (Git repository)

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
- **Handler**: GetProductHandler, CreateProductHandler, ListProductsHandler
- **DTOs**: ProductDto, CreateProductDto, UpdateProductDto
- **API Endpoint**: `/api/products`

### 2. Orders Management
- **Entity**: Order (ID, OrderDate, TotalAmount, Status)
- **Handlers**: CreateOrderHandler, GetOrderHandler, UpdateOrderHandler, DeleteOrderHandler
- **Relations**: Contains multiple OrderItems
- **DTOs**: CreateOrderDto, OrderResponseDto, UpdateOrderDto
- **API Endpoint**: `/api/orders`

### 3. Payments Processing
- **Entity**: Payment (ID, Amount, Status, PaymentMethod)
- **Handler**: ProcessPaymentHandler, GetPaymentStatusHandler
- **Relations**: Links to Orders
- **DTOs**: PaymentRequestDto, PaymentResponseDto
- **API Endpoint**: `/api/payments`

### 4. Authentication & Authorization
- **Handlers**: LoginHandler, RegisterHandler, VerifyEmailHandler, ForgotPasswordHandler, ResetPasswordHandler
- **DTOs**: LoginRequest, LoginResponse, RegisterRequest, VerifyEmailRequest, ForgotPasswordRequest, ResetPasswordRequest
- **Services**: IJwtProvider, IEmailService (in Infrastructure)
- **API Endpoint**: `/api/auth`

## Dependency Inversion Principle (DIP)

### Repository Interfaces Location: STEM.Core/Interfaces
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
