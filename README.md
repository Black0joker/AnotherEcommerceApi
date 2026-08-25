# E-Commerce API

A production-style e-commerce REST API built with ASP.NET Core 10, demonstrating Clean Architecture, security, transactions, concurrency handling, caching, and comprehensive testing.

## Tech Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10 / ASP.NET Core 10 |
| Database | SQL Server + Entity Framework Core 10 |
| Authentication | ASP.NET Core Identity + JWT Bearer |
| Cache | Redis |
| Validation | FluentValidation |
| Mediator | MediatR |
| API Docs | OpenAPI / Swagger |
| Testing | xUnit |
| Containerization | Docker + Docker Compose |
| CI | GitHub Actions |

## Architecture

```
src/
├── ECommerce.Api/            # Controllers, middleware, configuration
├── ECommerce.Application/    # Features (CQRS), validators, behaviors, pricing
├── ECommerce.Domain/         # Entities, enums, events, exceptions, value objects
└── ECommerce.Infrastructure/ # EF Core, Identity, Redis, storage, background jobs

tests/
├── ECommerce.UnitTests/          # Domain rules, validators, discount calculator
├── ECommerce.IntegrationTests/   # Full HTTP pipeline tests
└── ECommerce.ArchitectureTests/  # Layer dependency enforcement
```

**Dependency direction:** Api → Application → Domain ← Infrastructure

## Quick Start

### With Docker (recommended)

```bash
docker compose up
```

This starts:
- **API** on `http://localhost:8080`
- **SQL Server** on `localhost:1433`
- **Redis** on `localhost:6379`

### Without Docker

Prerequisites: .NET 10 SDK, SQL Server, Redis

```bash
# Restore and build
dotnet build ECommerce.slnx

# Run (Development mode auto-applies migrations and seeds data)
dotnet run --project src/ECommerce.Api
```

## API Documentation

When running in Development mode, Swagger UI is available at:

```
http://localhost:5000/swagger
```

OpenAPI spec: `http://localhost:5000/openapi/v1.json`

## Key Endpoints

| Area | Endpoints |
|------|-----------|
| Auth | `POST /api/v1/auth/register`, `/login`, `/refresh`, `/logout`, `GET /me` |
| Products | `GET/POST/PUT/DELETE /api/v1/products` |
| Categories | `GET/POST/PUT/DELETE /api/v1/categories` |
| Cart | `GET/POST/PUT/DELETE /api/v1/cart` |
| Wishlist | `GET/POST/DELETE /api/v1/wishlist` |
| Orders | `GET /api/v1/orders`, `POST /checkout`, `POST /{id}/cancel` |
| Reviews | `GET/POST /api/v1/products/{id}/reviews`, `PUT/DELETE /api/v1/reviews/{id}` |
| Admin | `/api/v1/admin/products`, `/orders`, `/users`, `/inventory`, `/discounts`, `/reports` |
| Health | `GET /health`, `GET /health/ready` |

## Running Tests

```bash
# All tests
dotnet test ECommerce.slnx

# Individual test projects
dotnet test tests/ECommerce.UnitTests
dotnet test tests/ECommerce.IntegrationTests
dotnet test tests/ECommerce.ArchitectureTests
```

## Key Design Decisions

- **Checkout transactions:** Inventory reservation, order creation, and cart cleanup are atomic (single DB transaction).
- **Optimistic concurrency:** `rowversion` on inventory prevents overselling under concurrent access.
- **Idempotency:** Checkout accepts an `Idempotency-Key` header; duplicate submissions return the existing result.
- **Server-side pricing:** Clients never control totals; all prices/discounts computed server-side.
- **Cache-aside:** Redis caches products/categories with invalidation on writes.
- **Refresh token rotation:** Each refresh issues a new token pair and revokes the old one.
- **Resource ownership:** Customers can only access their own orders (IDOR protection).

## Configuration

| Setting | Source |
|---------|--------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `ConnectionStrings:Redis` | Redis connection |
| `JwtSettings:*` | Token signing key, issuer, audience, expiry |
| `RateLimiting:*` | Per-minute request limits (global, auth, reviews) |

Production secrets must be provided via environment variables or a secrets manager — never committed to source control.

## Default Seed Data (Development only)

- Admin user: `admin@ecommerce.com` / `Admin@123!`
- Customer user: `customer@ecommerce.com` / `Customer@123!`
- Sample categories, products, inventory, and discounts
